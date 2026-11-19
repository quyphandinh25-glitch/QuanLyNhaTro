using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using DoAn.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAn.ViewModels
{
    public partial class HopDongViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<HopDong> _danhSachHopDong = new();
        [ObservableProperty] private ObservableCollection<Phong> _danhSachPhongTrong = new();
        [ObservableProperty] private ObservableCollection<KhachThue> _danhSachKhachThue = new();

        [ObservableProperty] private HopDong? _hopDongDangChon;

        [ObservableProperty] private string _maHopDong = string.Empty;
        [ObservableProperty] private Phong? _phongChon;
        [ObservableProperty] private KhachThue? _khachChon;
        [ObservableProperty] private DateTime _ngayBatDau = DateTime.Today;
        [ObservableProperty] private DateTime _ngayKetThuc = DateTime.Today.AddMonths(12);
        [ObservableProperty] private decimal _giaThueChot;
        [ObservableProperty] private decimal _tienCoc;
        [ObservableProperty] private int _soNguoiO = 1;
        [ObservableProperty] private int _soLuongXe = 1;

        public HopDongViewModel(AppDbContext db)
        {
            _db = db;
            TaiDuLieu();
            LamMoi();
        }

        public void TaiDuLieu()
        {
            // 1. Tự động đồng bộ trạng thái phòng khớp với hợp đồng hiệu lực
            var tatCaPhong = _db.Phongs.Include(p => p.HopDongs).ToList();
            bool coThayDoi = false;
            foreach (var p in tatCaPhong)
            {
                if (p.TrangThai != 3) // Không ghi đè phòng đang Bảo trì
                {
                    var hdHieuLuc = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                    int ttMoi = 0;
                    if (hdHieuLuc != null)
                    {
                        double soNgayConLai = (hdHieuLuc.NgayKetThuc.Date - DateTime.Today).TotalDays;
                        ttMoi = soNgayConLai <= 15 ? 2 : 1;
                    }

                    if (p.TrangThai != ttMoi)
                    {
                        p.TrangThai = ttMoi;
                        coThayDoi = true;
                    }
                }
            }
            if (coThayDoi) _db.SaveChanges();

            // 2. Nạp danh sách hợp đồng (ưu tiên hợp đồng Đang hiệu lực lên đầu)
            DanhSachHopDong = new ObservableCollection<HopDong>(
                _db.HopDongs
                   .Include(h => h.Phong)
                   .Include(h => h.KhachDaiDien)
                   .OrderBy(h => h.TrangThai)
                   .ThenByDescending(h => h.Id)
                   .ToList());

            // 3. Chỉ lấy phòng thực sự Trống (TrangThai == 0 và không có HĐ hiệu lực)
            DanhSachPhongTrong = new ObservableCollection<Phong>(
                _db.Phongs
                   .Where(p => p.TrangThai == 0 && !p.HopDongs.Any(h => h.TrangThai == 0))
                   .OrderBy(p => p.MaPhong)
                   .ToList());

            DanhSachKhachThue = new ObservableCollection<KhachThue>(
                _db.KhachThues.OrderBy(k => k.HoTen).ToList());
        }

        // Khi chọn phòng trống -> Tự động lấy đơn giá phòng làm Giá thuê chốt & Tiền cọc
        partial void OnPhongChonChanged(Phong? value)
        {
            if (value != null)
            {
                GiaThueChot = value.GiaThueHienTai;
                TienCoc = value.GiaThueHienTai;
            }
            else
            {
                GiaThueChot = 0;
                TienCoc = 0;
            }
        }

        // Sinh mã hợp đồng duy nhất, không bị trùng ngay cả khi từng xóa bản ghi
        private void TaoMaHopDongMoi()
        {
            int stt = _db.HopDongs.Count() + 1;
            string maDeXuat;
            do
            {
                maDeXuat = $"HD-{DateTime.Now:yyyy}-{stt:D3}";
                stt++;
            }
            while (_db.HopDongs.Any(h => h.MaHopDong.ToLower() == maDeXuat.ToLower()));

            MaHopDong = maDeXuat;
        }

        [RelayCommand]
        private void LapHopDong()
        {
            string maChuan = MaHopDong.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(maChuan))
            {
                MessageBox.Show("Vui lòng nhập Mã hợp đồng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PhongChon == null || KhachChon == null)
            {
                MessageBox.Show("Vui lòng chọn Phòng đang trống và Khách thuê đại diện!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NgayKetThuc.Date <= NgayBatDau.Date)
            {
                MessageBox.Show("Ngày kết thúc hợp đồng phải lớn hơn Ngày bắt đầu!", "Ngày không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (GiaThueChot <= 0 || TienCoc < 0)
            {
                MessageBox.Show("Giá thuê chốt phải lớn hơn 0 và Tiền đặt cọc không được âm!", "Số tiền không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SoNguoiO < 1 || SoLuongXe < 0)
            {
                MessageBox.Show("Số người ở phải từ 1 trở lên và Số lượng xe không được âm!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db.HopDongs.Any(h => h.MaHopDong.ToLower() == maChuan.ToLower()))
            {
                MessageBox.Show($"Mã hợp đồng '{maChuan}' đã tồn tại! Vui lòng đổi mã khác hoặc bấm Làm mới.", "Trùng mã hợp đồng", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Chặn ký đè lên phòng đang có hợp đồng hiệu lực hoặc đang bảo trì
            bool phongDangCoHopDong = _db.HopDongs.Any(h => h.PhongId == PhongChon.Id && h.TrangThai == 0);
            if (phongDangCoHopDong || PhongChon.TrangThai != 0)
            {
                MessageBox.Show($"Phòng {PhongChon.MaPhong} hiện không còn trống! Vui lòng làm mới danh sách.", "Phòng không khả dụng", MessageBoxButton.OK, MessageBoxImage.Error);
                TaiDuLieu();
                return;
            }

            var hopDongMoi = new HopDong
            {
                MaHopDong = maChuan,
                PhongId = PhongChon.Id,
                KhachDaiDienId = KhachChon.Id,
                NgayLap = DateTime.Now,
                NgayBatDau = NgayBatDau.Date,
                NgayKetThuc = NgayKetThuc.Date,
                GiaThueChot = GiaThueChot,
                TienCoc = TienCoc,
                SoNguoiO = SoNguoiO,
                SoLuongXe = SoLuongXe,
                TrangThai = 0 // 0 = Đang hiệu lực
            };

            // Đồng bộ trạng thái phòng: Đang thuê (1) hoặc Sắp hết hạn (2) nếu kỳ hạn <= 15 ngày
            double soNgayConLai = (NgayKetThuc.Date - DateTime.Today).TotalDays;
            PhongChon.TrangThai = soNgayConLai <= 15 ? 2 : 1;

            _db.HopDongs.Add(hopDongMoi);
            _db.SaveChanges();

            string maPhongVuaKy = PhongChon.MaPhong;
            string tenKhachVuaKy = KhachChon.HoTen;
            int idHdMoi = hopDongMoi.Id;

            TaiDuLieu();
            LamMoi();
            HopDongDangChon = DanhSachHopDong.FirstOrDefault(h => h.Id == idHdMoi);

            MessageBox.Show($"Đã ký hợp đồng {maChuan} cho khách {tenKhachVuaKy} nhận phòng {maPhongVuaKy} thành công!",
                "Lập hợp đồng thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void GiaHan6Thang()
        {
            if (HopDongDangChon == null || HopDongDangChon.TrangThai != 0)
            {
                MessageBox.Show("Vui lòng chọn một hợp đồng đang hiệu lực trên bảng để gia hạn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hd = HopDongDangChon;
            DateTime hanMoi = hd.NgayKetThuc.AddMonths(6);

            if (MessageBox.Show(
                $"Xác nhận gia hạn hợp đồng {hd.MaHopDong} (Phòng {hd.Phong?.MaPhong}):\n\n" +
                $"• Hạn hiện tại: {hd.NgayKetThuc:dd/MM/yyyy}\n" +
                $"• Hạn mới (+6 tháng): {hanMoi:dd/MM/yyyy}",
                "Xác nhận Gia hạn Hợp đồng", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            int idHd = hd.Id;
            string maHd = hd.MaHopDong;

            hd.NgayKetThuc = hanMoi;
            if (hd.Phong != null && hd.Phong.TrangThai != 3)
            {
                double soNgayConLai = (hanMoi.Date - DateTime.Today).TotalDays;
                hd.Phong.TrangThai = soNgayConLai <= 15 ? 2 : 1;
            }

            _db.SaveChanges();
            TaiDuLieu();
            HopDongDangChon = DanhSachHopDong.FirstOrDefault(h => h.Id == idHd);

            MessageBox.Show($"Đã gia hạn hợp đồng {maHd} đến ngày {hanMoi:dd/MM/yyyy}!",
                "Gia hạn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void TraPhongThanhLy()
        {
            if (HopDongDangChon == null || HopDongDangChon.TrangThai != 0)
            {
                MessageBox.Show("Vui lòng chọn một hợp đồng đang hiệu lực trên bảng để làm thủ tục trả phòng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hd = HopDongDangChon;

            // Tính chuẩn tổng nợ gốc thực tế chưa thanh toán (không cộng trùng NoCu giữa các kỳ)
            decimal tongNoHoaDon = Math.Max(0, _db.HoaDons
                .Where(b => b.HopDongId == hd.Id)
                .AsEnumerable()
                .Sum(b => (b.TongCong - b.NoCu) - b.SoTienDaDong));

            decimal tienHoanCoc = hd.TienCoc - tongNoHoaDon;

            string dongKetLuan = tienHoanCoc >= 0
                ? $"👉 SỐ TIỀN HOÀN CỌC CHO KHÁCH: {tienHoanCoc:N0} VNĐ"
                : $"⚠️ KHÁCH CẦN ĐÓNG THÊM (SAU KHI TRỪ CỌC): {Math.Abs(tienHoanCoc):N0} VNĐ";

            string thongBao = $"Xác nhận thanh lý hợp đồng {hd.MaHopDong} (Phòng {hd.Phong?.MaPhong} - {hd.KhachDaiDien?.HoTen}):\n\n" +
                              $"• Tiền cọc đang giữ: {hd.TienCoc:N0} VNĐ\n" +
                              $"• Tổng nợ hóa đơn còn lại: {tongNoHoaDon:N0} VNĐ\n" +
                              $"{dongKetLuan}\n\n" +
                              $"Bạn có chắc chắn muốn hoàn tất trả phòng?";

            if (MessageBox.Show(thongBao, "Thanh lý Hợp đồng & Hoàn cọc", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int idHd = hd.Id;
                string maPhong = hd.Phong?.MaPhong ?? "";

                hd.TrangThai = 1; // 1 = Đã thanh lý / Đóng
                hd.TienHoanCoc = tienHoanCoc;
                hd.NgayTraPhongThucTe = DateTime.Now;

                // Trả phòng về Trống (0) nếu không đang ở trạng thái Bảo trì (3)
                if (hd.Phong != null && hd.Phong.TrangThai != 3)
                {
                    hd.Phong.TrangThai = 0;
                }

                _db.SaveChanges();
                TaiDuLieu();
                LamMoi();
                HopDongDangChon = DanhSachHopDong.FirstOrDefault(h => h.Id == idHd);

                MessageBox.Show($"Đã thanh lý hợp đồng và chuyển phòng {maPhong} về trạng thái Phòng trống!",
                    "Hoàn tất trả phòng", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            HopDongDangChon = null;
            TaoMaHopDongMoi();
            PhongChon = DanhSachPhongTrong.FirstOrDefault();
            KhachChon = DanhSachKhachThue.FirstOrDefault();
            NgayBatDau = DateTime.Today;
            NgayKetThuc = DateTime.Today.AddMonths(12);
            SoNguoiO = 1;
            SoLuongXe = 1;
        }
    }
}