using System;
using System.Collections.Generic;
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
    // Class chứa dữ liệu hiển thị cho từng ô phòng trên Sơ đồ
    public class PhongSoDoItem
    {
        public Phong PhongGoc { get; set; } = null!;
        public int Id => PhongGoc.Id;
        public string MaPhong => PhongGoc.MaPhong;
        public string TenPhong => PhongGoc.TenPhong;
        public int Tang => PhongGoc.Tang;
        public string TenLoaiPhong => PhongGoc.LoaiPhong?.TenLoai ?? "Tiêu chuẩn";
        public decimal GiaThue => PhongGoc.GiaThueHienTai;
        public int TrangThai => PhongGoc.TrangThai; // 0: Trống, 1: Đang thuê, 2: Sắp hết hạn, 3: Bảo trì

        public string TenTrangThai => TrangThai switch
        {
            0 => "🟢 Phòng trống",
            1 => "🔵 Đang thuê",
            2 => "🟠 Sắp hết hạn",
            3 => "🔴 Đang bảo trì",
            _ => "Không xác định"
        };

        public string TenKhachHienTai { get; set; } = "(Chưa có khách)";
        public string SoDienThoaiKhach { get; set; } = "---";
        public string HanHopDongText { get; set; } = "---";
        public decimal TienCocHienTai { get; set; }
        public HopDong? HopDongHienTai { get; set; }
        public string GhiChuSuCo { get; set; } = "Không có sự cố";
    }

    public partial class SoDoPhongViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<PhongSoDoItem> _danhSachPhongSoDo = new();
        [ObservableProperty] private PhongSoDoItem? _phongDangChon;

        // Bộ lọc trên thanh công cụ
        public List<string> DanhSachTangLoc { get; } = new() { "Tất cả tầng", "Tầng 1", "Tầng 2", "Tầng 3" };
        public List<string> DanhSachTrangThaiLoc { get; } = new() { "Tất cả trạng thái", "0 - Phòng trống", "1 - Đang thuê", "2 - Sắp hết hạn", "3 - Đang bảo trì" };

        [ObservableProperty] private string _tangLocChon = "Tất cả tầng";
        [ObservableProperty] private string _trangThaiLocChon = "Tất cả trạng thái";
        [ObservableProperty] private string _tuKhoaTimKiem = string.Empty;

        // Thống kê nhanh trên thanh tiêu đề sơ đồ
        [ObservableProperty] private int _demTong;
        [ObservableProperty] private int _demTrong;
        [ObservableProperty] private int _demDangThue;
        [ObservableProperty] private int _demSapHet;
        [ObservableProperty] private int _demBaoTri;

        // Dữ liệu phục vụ Thao tác nhanh bên Panel phải
        [ObservableProperty] private ObservableCollection<KhachThue> _danhSachKhachThue = new();
        [ObservableProperty] private KhachThue? _khachThueNhanhChon;
        [ObservableProperty] private string _noiDungBaoSuCo = string.Empty;

        public SoDoPhongViewModel(AppDbContext db)
        {
            _db = db;
            TaiSoDoPhong();
        }

        partial void OnTangLocChonChanged(string value) => TaiSoDoPhong();
        partial void OnTrangThaiLocChonChanged(string value) => TaiSoDoPhong();
        partial void OnTuKhoaTimKiemChanged(string value) => TaiSoDoPhong();

        public void TaiSoDoPhong()
        {
            var tatCaPhong = _db.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.HopDongs).ThenInclude(h => h.KhachDaiDien)
                .Include(p => p.SuCos)
                .OrderBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .ToList();

            // Tự động đồng bộ trạng thái Sắp hết hạn (còn <= 15 ngày)
            foreach (var p in tatCaPhong)
            {
                var hdHieuLuc = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                if (hdHieuLuc != null && p.TrangThai != 3)
                {
                    double soNgayConLai = (hdHieuLuc.NgayKetThuc - DateTime.Now).TotalDays;
                    p.TrangThai = soNgayConLai <= 15 ? 2 : 1;
                }
            }
            _db.SaveChanges();

            // Cập nhật bộ đếm tổng quát
            DemTong = tatCaPhong.Count;
            DemTrong = tatCaPhong.Count(p => p.TrangThai == 0);
            DemDangThue = tatCaPhong.Count(p => p.TrangThai == 1);
            DemSapHet = tatCaPhong.Count(p => p.TrangThai == 2);
            DemBaoTri = tatCaPhong.Count(p => p.TrangThai == 3);

            // Áp dụng bộ lọc
            var ketQuaLoc = tatCaPhong.AsEnumerable();

            if (TangLocChon == "Tầng 1") ketQuaLoc = ketQuaLoc.Where(p => p.Tang == 1);
            else if (TangLocChon == "Tầng 2") ketQuaLoc = ketQuaLoc.Where(p => p.Tang == 2);
            else if (TangLocChon == "Tầng 3") ketQuaLoc = ketQuaLoc.Where(p => p.Tang == 3);

            if (TrangThaiLocChon.StartsWith("0")) ketQuaLoc = ketQuaLoc.Where(p => p.TrangThai == 0);
            else if (TrangThaiLocChon.StartsWith("1")) ketQuaLoc = ketQuaLoc.Where(p => p.TrangThai == 1);
            else if (TrangThaiLocChon.StartsWith("2")) ketQuaLoc = ketQuaLoc.Where(p => p.TrangThai == 2);
            else if (TrangThaiLocChon.StartsWith("3")) ketQuaLoc = ketQuaLoc.Where(p => p.TrangThai == 3);

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQuaLoc = ketQuaLoc.Where(p =>
                    p.MaPhong.ToLower().Contains(kw) ||
                    p.TenPhong.ToLower().Contains(kw) ||
                    p.HopDongs.Any(h => h.TrangThai == 0 && h.KhachDaiDien != null && h.KhachDaiDien.HoTen.ToLower().Contains(kw)));
            }

            var danhSachItems = new List<PhongSoDoItem>();
            foreach (var p in ketQuaLoc)
            {
                var hd = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                var suCo = p.SuCos.FirstOrDefault(s => s.TrangThai != 2);

                danhSachItems.Add(new PhongSoDoItem
                {
                    PhongGoc = p,
                    HopDongHienTai = hd,
                    TenKhachHienTai = hd?.KhachDaiDien?.HoTen ?? (p.TrangThai == 3 ? "Đang sửa chữa" : "(Phòng trống)"),
                    SoDienThoaiKhach = hd?.KhachDaiDien?.SoDienThoai ?? "---",
                    HanHopDongText = hd != null ? $"{hd.NgayBatDau:dd/MM/yyyy} - {hd.NgayKetThuc:dd/MM/yyyy}" : "---",
                    TienCocHienTai = hd?.TienCoc ?? 0,
                    GhiChuSuCo = suCo != null ? $"{suCo.TieuDe} ({suCo.ChiPhiSuaChua:N0}đ)" : (p.GhiChu ?? "Bình thường")
                });
            }

            DanhSachPhongSoDo = new ObservableCollection<PhongSoDoItem>(danhSachItems);
            PhongDangChon = DanhSachPhongSoDo.FirstOrDefault();

            DanhSachKhachThue = new ObservableCollection<KhachThue>(_db.KhachThues.ToList());
            KhachThueNhanhChon = DanhSachKhachThue.FirstOrDefault();
        }

        // THAO TÁC NHANH 1: Cho thuê nhanh từ ô phòng trống
        [RelayCommand]
        private void ChoThueNhanh()
        {
            if (PhongDangChon == null || PhongDangChon.TrangThai != 0)
            {
                MessageBox.Show("Chỉ có thể lập hợp đồng cho thuê nhanh đối với Phòng Trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (KhachThueNhanhChon == null)
            {
                MessageBox.Show("Vui lòng chọn Khách thuê trong danh sách!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int soHd = _db.HopDongs.Count() + 1;
            var hdMoi = new HopDong
            {
                MaHopDong = $"HD-{DateTime.Now:yyyy}-{soHd:D3}",
                PhongId = PhongDangChon.Id,
                KhachDaiDienId = KhachThueNhanhChon.Id,
                NgayLap = DateTime.Now,
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddMonths(12),
                GiaThueChot = PhongDangChon.GiaThue,
                TienCoc = PhongDangChon.GiaThue,
                SoNguoiO = 1,
                SoLuongXe = 1,
                TrangThai = 0
            };

            PhongDangChon.PhongGoc.TrangThai = 1;
            _db.HopDongs.Add(hdMoi);
            _db.SaveChanges();

            int idVuaChon = PhongDangChon.Id;
            TaiSoDoPhong();
            PhongDangChon = DanhSachPhongSoDo.FirstOrDefault(x => x.Id == idVuaChon);

            MessageBox.Show($"Đã cho khách {KhachThueNhanhChon.HoTen} thuê phòng {PhongDangChon?.MaPhong} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // THAO TÁC NHANH 2: Gia hạn nhanh hợp đồng +6 tháng
        [RelayCommand]
        private void GiaHanNhanh()
        {
            if (PhongDangChon?.HopDongHienTai == null)
            {
                MessageBox.Show("Phòng này hiện không có hợp đồng đang hiệu lực để gia hạn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PhongDangChon.HopDongHienTai.NgayKetThuc = PhongDangChon.HopDongHienTai.NgayKetThuc.AddMonths(6);
            PhongDangChon.PhongGoc.TrangThai = 1;
            _db.SaveChanges();

            int idVuaChon = PhongDangChon.Id;
            TaiSoDoPhong();
            PhongDangChon = DanhSachPhongSoDo.FirstOrDefault(x => x.Id == idVuaChon);

            MessageBox.Show("Đã gia hạn hợp đồng thêm 6 tháng! Trạng thái phòng đã cập nhật.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // THAO TÁC NHANH 3: Trả phòng & Thanh lý nhanh ngay trên Sơ đồ
        [RelayCommand]
        private void TraPhongNhanh()
        {
            if (PhongDangChon?.HopDongHienTai == null)
            {
                MessageBox.Show("Phòng này chưa có hợp đồng thuê hiệu lực!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hd = PhongDangChon.HopDongHienTai;
            decimal noHoaDon = _db.HoaDons.Where(b => b.HopDongId == hd.Id && b.TrangThai != 2).Sum(b => b.ConNo);
            decimal tienHoan = hd.TienCoc - noHoaDon;

            if (MessageBox.Show(
                $"Xác nhận trả phòng {PhongDangChon.MaPhong} (Khách: {PhongDangChon.TenKhachHienTai}):\n" +
                $"• Tiền cọc: {hd.TienCoc:N0} VNĐ\n" +
                $"• Nợ hóa đơn: {noHoaDon:N0} VNĐ\n" +
                $"👉 Hoàn trả khách: {tienHoan:N0} VNĐ\n\n" +
                $"Bạn muốn hoàn tất trả phòng?",
                "Xác nhận Trả phòng", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                hd.TrangThai = 1;
                hd.TienHoanCoc = tienHoan;
                hd.NgayTraPhongThucTe = DateTime.Now;
                PhongDangChon.PhongGoc.TrangThai = 0;

                _db.SaveChanges();
                int idVuaChon = PhongDangChon.Id;
                TaiSoDoPhong();
                PhongDangChon = DanhSachPhongSoDo.FirstOrDefault(x => x.Id == idVuaChon);
            }
        }

        // THAO TÁC NHANH 4: Báo sự cố / Chuyển trạng thái Bảo trì hoặc Hoàn tất bảo trì
        [RelayCommand]
        private void ChuyenTrangThaiBaoTri()
        {
            if (PhongDangChon == null) return;

            // Nếu đang Bảo trì (3) -> Mở lại phòng về Trống (0) hoặc Đang thuê (1)
            if (PhongDangChon.TrangThai == 3)
            {
                var suCo = _db.SuCos.Where(s => s.PhongId == PhongDangChon.Id && s.TrangThai != 2).ToList();
                foreach (var s in suCo) s.TrangThai = 2;

                bool coHopDong = _db.HopDongs.Any(h => h.PhongId == PhongDangChon.Id && h.TrangThai == 0);
                PhongDangChon.PhongGoc.TrangThai = coHopDong ? 1 : 0;
                PhongDangChon.PhongGoc.GhiChu = "Đã hoàn tất bảo trì";
                _db.SaveChanges();

                int id = PhongDangChon.Id;
                TaiSoDoPhong();
                PhongDangChon = DanhSachPhongSoDo.FirstOrDefault(x => x.Id == id);
                MessageBox.Show("Đã hoàn tất bảo trì và mở lại phòng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string moTa = string.IsNullOrWhiteSpace(NoiDungBaoSuCo) ? "Bảo trì định kỳ / Sửa chữa thiết bị" : NoiDungBaoSuCo.Trim();
                _db.SuCos.Add(new SuCo
                {
                    PhongId = PhongDangChon.Id,
                    TieuDe = moTa,
                    MoTaChiTiet = "Ghi nhận trực tiếp từ Sơ đồ phòng",
                    ChiPhiSuaChua = 0,
                    TrangThai = 1
                });

                PhongDangChon.PhongGoc.TrangThai = 3;
                PhongDangChon.PhongGoc.GhiChu = moTa;
                _db.SaveChanges();

                NoiDungBaoSuCo = string.Empty;
                int id = PhongDangChon.Id;
                TaiSoDoPhong();
                PhongDangChon = DanhSachPhongSoDo.FirstOrDefault(x => x.Id == id);
                MessageBox.Show("Đã chuyển phòng sang trạng thái Đang bảo trì!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}