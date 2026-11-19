using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using DoAn.Models;
using DoAn.Services;
using Microsoft.EntityFrameworkCore;

namespace DoAn.ViewModels
{
    public partial class HoaDonViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public List<int> DanhSachThang { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> DanhSachNam { get; } = new() { 2025, 2026, 2027, 2028 };

        // Bộ lọc theo dõi Công nợ
        public List<string> DanhSachLocCongNo { get; } = new()
        {
            "Tất cả hóa đơn",
            "⚠️ Tất cả đang còn nợ",
            "0 - Chưa thanh toán",
            "1 - Đóng một phần (Còn nợ)",
            "2 - Đã thanh toán đủ"
        };

        public List<string> DanhSachPhuongThuc { get; } = new() { "Chuyển khoản", "Tiền mặt" };

        // Giữ nguyên các [ObservableProperty] gốc để tránh xung đột Source Generator
        [ObservableProperty] private int _thangChon = DateTime.Now.Month;
        [ObservableProperty] private int _namChon = DateTime.Now.Year;
        [ObservableProperty] private string _locCongNoChon = "Tất cả hóa đơn";

        [ObservableProperty] private ObservableCollection<HoaDon> _danhSachHoaDon = new();
        [ObservableProperty] private HoaDon? _hoaDonDangChon;

        [ObservableProperty] private decimal _soTienKhachDong;
        [ObservableProperty] private string _phuongThucChon = "Chuyển khoản";
        [ObservableProperty] private string _ghiChuThanhToan = string.Empty;
        [ObservableProperty] private ObservableCollection<ThanhToan> _lichSuThanhToan = new();

        [ObservableProperty] private decimal _tongPhaiThuThang;
        [ObservableProperty] private decimal _tongDaThuThang;
        [ObservableProperty] private decimal _tongConNoThang;

        // Các thuộc tính mới khai báo tường minh để không bao giờ bị CS0103
        private string _tuKhoaTimKiem = string.Empty;
        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set
            {
                if (SetProperty(ref _tuKhoaTimKiem, value))
                    TaiDanhSachHoaDon();
            }
        }

        private bool _coTheThuTien;
        public bool CoTheThuTien
        {
            get => _coTheThuTien;
            set => SetProperty(ref _coTheThuTien, value);
        }

        private bool _coTheXoaHoaDon;
        public bool CoTheXoaHoaDon
        {
            get => _coTheXoaHoaDon;
            set => SetProperty(ref _coTheXoaHoaDon, value);
        }

        private Visibility _hienThiDaThanhToanDu = Visibility.Collapsed;
        public Visibility HienThiDaThanhToanDu
        {
            get => _hienThiDaThanhToanDu;
            set => SetProperty(ref _hienThiDaThanhToanDu, value);
        }

        private string _soLuongHoaDonText = "0 hóa đơn";
        public string SoLuongHoaDonText
        {
            get => _soLuongHoaDonText;
            set => SetProperty(ref _soLuongHoaDonText, value);
        }

        public HoaDonViewModel(AppDbContext db)
        {
            _db = db;
            TaiDanhSachHoaDon();
        }

        partial void OnThangChonChanged(int value) => TaiDanhSachHoaDon();
        partial void OnNamChonChanged(int value) => TaiDanhSachHoaDon();
        partial void OnLocCongNoChonChanged(string value) => TaiDanhSachHoaDon();

        // Khi chọn 1 hóa đơn trên bảng -> Tự động gợi ý số tiền còn nợ, khóa nút nếu đã đóng đủ & tải lịch sử
        partial void OnHoaDonDangChonChanged(HoaDon? value)
        {
            if (value != null)
            {
                SoTienKhachDong = value.ConNo;
                GhiChuThanhToan = string.Empty;

                bool daDongDu = value.TrangThai == 2 || value.ConNo <= 0;
                CoTheThuTien = !daDongDu;
                CoTheXoaHoaDon = value.TrangThai != 2 && value.SoTienDaDong == 0;
                HienThiDaThanhToanDu = daDongDu ? Visibility.Visible : Visibility.Collapsed;

                var lichSu = _db.ThanhToans
                    .Where(t => t.HoaDonId == value.Id)
                    .OrderByDescending(t => t.NgayThanhToan)
                    .ToList();
                LichSuThanhToan = new ObservableCollection<ThanhToan>(lichSu);
            }
            else
            {
                SoTienKhachDong = 0;
                CoTheThuTien = false;
                CoTheXoaHoaDon = false;
                HienThiDaThanhToanDu = Visibility.Collapsed;
                LichSuThanhToan = new ObservableCollection<ThanhToan>();
            }
        }

        public void TaiDanhSachHoaDon()
        {
            var tatCaTrongKy = _db.HoaDons
                .Include(h => h.Phong)
                .Include(h => h.HopDong).ThenInclude(hd => hd!.KhachDaiDien)
                .Where(h => h.Thang == ThangChon && h.Nam == NamChon)
                .OrderBy(h => h.Phong!.MaPhong)
                .ToList();

            // Tính tổng thống kê của cả tháng (không phụ thuộc bộ lọc)
            TongPhaiThuThang = tatCaTrongKy.Sum(x => x.TongCong);
            TongDaThuThang = tatCaTrongKy.Sum(x => x.SoTienDaDong);
            TongConNoThang = tatCaTrongKy.Sum(x => x.ConNo);

            // Áp dụng bộ lọc Công nợ & Tìm kiếm
            var ketQuaLoc = tatCaTrongKy.AsEnumerable();
            if (LocCongNoChon.StartsWith("⚠️"))
                ketQuaLoc = ketQuaLoc.Where(h => h.ConNo > 0);
            else if (LocCongNoChon.StartsWith("0"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 0);
            else if (LocCongNoChon.StartsWith("1"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 1);
            else if (LocCongNoChon.StartsWith("2"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 2);

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQuaLoc = ketQuaLoc.Where(h =>
                    (h.MaHoaDon ?? "").ToLower().Contains(kw) ||
                    (h.Phong?.MaPhong ?? "").ToLower().Contains(kw) ||
                    (h.HopDong?.KhachDaiDien?.HoTen ?? "").ToLower().Contains(kw));
            }

            var dsLoc = ketQuaLoc.ToList();
            DanhSachHoaDon = new ObservableCollection<HoaDon>(dsLoc);
            SoLuongHoaDonText = $"Hiển thị {dsLoc.Count} / {tatCaTrongKy.Count} hóa đơn";
            HoaDonDangChon = DanhSachHoaDon.FirstOrDefault();
        }

        // Lập & cập nhật hóa đơn hàng loạt (giữ nguyên logic chuẩn: lọc HĐ trong kỳ, chặn tháng tương lai, nợ cũ gốc không cộng trùng)
        [RelayCommand]
        private void LapHoaDonHangLoat()
        {
            DateTime ngayDauThang = new DateTime(NamChon, ThangChon, 1);
            DateTime ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            // Chặn lập hóa đơn cho tháng trong tương lai
            DateTime thangHienTai = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (ngayDauThang > thangHienTai)
            {
                MessageBox.Show($"Không thể lập hóa đơn cho Tháng {ThangChon}/{NamChon} vì chưa đến kỳ!",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lọc theo Hợp đồng đang hiệu lực giao với tháng đang chọn
            var hopDongHieuLuc = _db.HopDongs
                .Include(h => h.Phong)
                .Where(h => h.TrangThai == 0
                         && h.NgayBatDau <= ngayCuoiThang
                         && h.NgayKetThuc >= ngayDauThang)
                .ToList();

            if (!hopDongHieuLuc.Any())
            {
                MessageBox.Show($"Không có hợp đồng nào đang lưu trú trong Tháng {ThangChon}/{NamChon}!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cauHinh = _db.NguoiDungs.FirstOrDefault();
            decimal giaDien = cauHinh?.DonGiaDienMacDinh ?? 3500;
            decimal giaNuoc = cauHinh?.DonGiaNuocMacDinh ?? 20000;
            var cacDichVu = _db.DichVus.Where(d => d.BatBuoc).ToList();

            int soTaoMoi = 0, soCapNhat = 0, soBoQuaDaDongDu = 0;

            foreach (var hd in hopDongHieuLuc)
            {
                decimal tienPhong = hd.GiaThueChot;
                if (hd.NgayBatDau.Year == NamChon && hd.NgayBatDau.Month == ThangChon && hd.NgayBatDau.Day > 5)
                {
                    int soNgayO = Math.Max(1, 30 - hd.NgayBatDau.Day + 1);
                    tienPhong = Math.Round((hd.GiaThueChot / 30m * soNgayO) / 1000m) * 1000m;
                }

                var chiSo = _db.ChiSoDienNuocs
                    .FirstOrDefault(c => c.PhongId == hd.PhongId && c.Thang == ThangChon && c.Nam == NamChon);

                int soDien = chiSo != null ? Math.Max(0, chiSo.ChiSoDienMoi - chiSo.ChiSoDienCu) : 0;
                int soNuoc = chiSo != null ? Math.Max(0, chiSo.ChiSoNuocMoi - chiSo.ChiSoNuocCu) : 0;

                decimal tienDien = soDien * giaDien;
                decimal tienNuoc = soNuoc * giaNuoc;

                decimal tienDichVu = 0;
                foreach (var dv in cacDichVu)
                {
                    if (dv.CachTinh == 0) tienDichVu += dv.DonGia;
                    else if (dv.CachTinh == 1) tienDichVu += dv.DonGia * hd.SoNguoiO;
                    else if (dv.CachTinh == 2) tienDichVu += dv.DonGia * hd.SoLuongXe;
                }

                // Tính chuẩn Nợ cũ gốc thực tế của các tháng TRƯỚC kỳ đang lập (không cộng trùng NoCu)
                decimal noCu = Math.Max(0, _db.HoaDons
                    .Where(b => b.HopDongId == hd.Id
                             && (b.Nam < NamChon || (b.Nam == NamChon && b.Thang < ThangChon)))
                    .AsEnumerable()
                    .Sum(b => (b.TongCong - b.NoCu) - b.SoTienDaDong));

                decimal tongCong = tienPhong + tienDien + tienNuoc + tienDichVu + noCu;

                var hoaDonHienTai = _db.HoaDons
                    .FirstOrDefault(b => b.PhongId == hd.PhongId && b.Thang == ThangChon && b.Nam == NamChon);

                if (hoaDonHienTai == null)
                {
                    _db.HoaDons.Add(new HoaDon
                    {
                        MaHoaDon = $"BILL-{ThangChon:D2}{NamChon}-{hd.Phong?.MaPhong}",
                        HopDongId = hd.Id,
                        PhongId = hd.PhongId,
                        Thang = ThangChon,
                        Nam = NamChon,
                        TienPhong = tienPhong,
                        TienDien = tienDien,
                        TienNuoc = tienNuoc,
                        TienDichVu = tienDichVu,
                        NoCu = noCu,
                        TongCong = tongCong,
                        SoTienDaDong = 0,
                        ConNo = tongCong,
                        TrangThai = 0,
                        NgayLap = DateTime.Now
                    });
                    soTaoMoi++;
                }
                else if (hoaDonHienTai.TrangThai != 2)
                {
                    hoaDonHienTai.TienPhong = tienPhong;
                    hoaDonHienTai.TienDien = tienDien;
                    hoaDonHienTai.TienNuoc = tienNuoc;
                    hoaDonHienTai.TienDichVu = tienDichVu;
                    hoaDonHienTai.NoCu = noCu;
                    hoaDonHienTai.TongCong = tongCong;
                    hoaDonHienTai.ConNo = Math.Max(0, tongCong - hoaDonHienTai.SoTienDaDong);
                    hoaDonHienTai.TrangThai = hoaDonHienTai.ConNo <= 0 ? 2 : (hoaDonHienTai.SoTienDaDong > 0 ? 1 : 0);
                    soCapNhat++;
                }
                else
                {
                    soBoQuaDaDongDu++;
                }
            }

            _db.SaveChanges();
            TaiDanhSachHoaDon();
            MessageBox.Show(
                $"Hoàn tất lập hóa đơn Tháng {ThangChon}/{NamChon}!\n" +
                $"• Tạo mới: {soTaoMoi} hóa đơn\n" +
                $"• Cập nhật: {soCapNhat} hóa đơn\n" +
                $"• Giữ nguyên (đã thanh toán đủ): {soBoQuaDaDongDu} hóa đơn",
                "Lập hóa đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Nghiệp vụ thu tiền nhiều đợt (tự cập nhật lịch sử ThanhToans và chuyển trạng thái 0 -> 1 -> 2)
        [RelayCommand]
        private void ThuTienHoaDon()
        {
            if (HoaDonDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn cần thu tiền!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HoaDonDangChon.TrangThai == 2 || HoaDonDangChon.ConNo <= 0)
            {
                MessageBox.Show("Hóa đơn này đã được thanh toán đủ và đã khóa chỉnh sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SoTienKhachDong <= 0)
            {
                MessageBox.Show("Số tiền thu phải lớn hơn 0 VNĐ!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SoTienKhachDong > HoaDonDangChon.ConNo)
            {
                MessageBox.Show($"Số tiền khách đóng ({SoTienKhachDong:N0} đ) không được vượt quá số tiền còn nợ ({HoaDonDangChon.ConNo:N0} đ)!",
                    "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. Lưu lịch sử giao dịch vào bảng ThanhToans
            var giaoDichMoi = new ThanhToan
            {
                HoaDonId = HoaDonDangChon.Id,
                NgayThanhToan = DateTime.Now,
                SoTienThu = SoTienKhachDong,
                PhuongThuc = PhuongThucChon,
                GhiChu = string.IsNullOrWhiteSpace(GhiChuThanhToan) ? $"Thu tiền kỳ T{ThangChon}/{NamChon}" : GhiChuThanhToan.Trim()
            };
            _db.ThanhToans.Add(giaoDichMoi);

            // 2. Cập nhật số tiền đã đóng, công nợ và trạng thái hóa đơn
            HoaDonDangChon.SoTienDaDong += SoTienKhachDong;
            HoaDonDangChon.ConNo = Math.Max(0, HoaDonDangChon.TongCong - HoaDonDangChon.SoTienDaDong);
            HoaDonDangChon.TrangThai = HoaDonDangChon.ConNo <= 0 ? 2 : 1;

            _db.SaveChanges();

            int idVuaChon = HoaDonDangChon.Id;
            decimal soTienVuaThu = SoTienKhachDong;
            bool daXong = HoaDonDangChon.TrangThai == 2;

            TaiDanhSachHoaDon();
            HoaDonDangChon = DanhSachHoaDon.FirstOrDefault(h => h.Id == idVuaChon);

            MessageBox.Show(
                daXong
                    ? $"Đã thu {soTienVuaThu:N0} VNĐ! Hóa đơn đã thanh toán đủ và chuyển sang trạng thái Hoàn tất."
                    : $"Đã ghi nhận đợt thu {soTienVuaThu:N0} VNĐ! Công nợ còn lại đã được cập nhật.",
                "Thu tiền thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Xem trước & In / Xuất PDF Phiếu báo tiền trọ sắc nét chuyên nghiệp
        [RelayCommand]
        private void InXuatPdfHoaDon()
        {
            if (HoaDonDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn để xem trước và xuất PDF!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            static SolidColorBrush Mau(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

            var hd = HoaDonDangChon;
            string tenNhaTro = AuthService.CurrentUser?.TenNhaTro ?? "Nhà Trọ 8386 Bình Định";
            string diaChi = AuthService.CurrentUser?.DiaChiNhaTro ?? "TP. Thủ Đức, TP. Hồ Chí Minh";
            string sdtChuTro = AuthService.CurrentUser?.SoDienThoai ?? "0909 123 456";
            string tenChuTro = AuthService.CurrentUser?.HoTen ?? "Chủ Trọ Quản Lý";

            // Truy xuất chỉ số điện nước trong kỳ để in chi tiết số cũ - mới lên phiếu
            var chiSo = _db.ChiSoDienNuocs.AsNoTracking()
                .FirstOrDefault(c => c.PhongId == hd.PhongId && c.Thang == hd.Thang && c.Nam == hd.Nam);

            string chiTietDien = chiSo != null
                ? $"Chỉ số cũ: {chiSo.ChiSoDienCu} → Mới: {chiSo.ChiSoDienMoi} ({Math.Max(0, chiSo.ChiSoDienMoi - chiSo.ChiSoDienCu)} kWh)"
                : "Theo chỉ số công tơ tháng";

            string chiTietNuoc = chiSo != null
                ? $"Chỉ số cũ: {chiSo.ChiSoNuocCu} → Mới: {chiSo.ChiSoNuocMoi} ({Math.Max(0, chiSo.ChiSoNuocMoi - chiSo.ChiSoNuocCu)} m³)"
                : "Theo chỉ số đồng hồ tháng";

            string trangThaiPhieu = hd.TrangThai switch
            {
                2 => "ĐÃ THANH TOÁN ĐỦ",
                1 => "THANH TOÁN MỘT PHẦN",
                _ => "CHƯA THANH TOÁN"
            };
            var mauTrangThai = hd.TrangThai == 2 ? Mau("#047857") : (hd.TrangThai == 1 ? Mau("#B45309") : Mau("#DC2626"));

            var doc = new FlowDocument
            {
                PagePadding = new Thickness(48, 40, 48, 40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5,
                Foreground = Mau("#0F172A"),
                ColumnWidth = double.PositiveInfinity
            };

            // 1. Header 2 cột: Thông tin Nhà trọ (Trái) & Mã phiếu + Trạng thái (Phải)
            var headerTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14) };
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(360) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(240) });
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();

            var leftHeader = new Paragraph { Margin = new Thickness(0) };
            leftHeader.Inlines.Add(new Bold(new Run(tenNhaTro.ToUpper() + "\n")) { FontSize = 16, Foreground = Mau("#1E3A8A") });
            leftHeader.Inlines.Add(new Run($"Địa chỉ: {diaChi}\n") { FontSize = 11.5, Foreground = Mau("#475569") });
            leftHeader.Inlines.Add(new Run($"Hotline hỗ trợ: {sdtChuTro}") { FontSize = 11.5, Foreground = Mau("#475569") });

            var rightHeader = new Paragraph { TextAlignment = TextAlignment.Right, Margin = new Thickness(0) };
            rightHeader.Inlines.Add(new Bold(new Run($"Mã phiếu: {hd.MaHoaDon}\n")) { FontSize = 12, Foreground = Mau("#0F172A") });
            rightHeader.Inlines.Add(new Run($"Ngày lập: {hd.NgayLap:dd/MM/yyyy HH:mm}\n") { FontSize = 11, Foreground = Mau("#64748B") });
            rightHeader.Inlines.Add(new Bold(new Run($"● {trangThaiPhieu}")) { FontSize = 11.5, Foreground = mauTrangThai });

            headerRow.Cells.Add(new TableCell(leftHeader) { BorderBrush = Mau("#1E3A8A"), BorderThickness = new Thickness(0, 0, 0, 2.5), Padding = new Thickness(0, 0, 0, 10) });
            headerRow.Cells.Add(new TableCell(rightHeader) { BorderBrush = Mau("#1E3A8A"), BorderThickness = new Thickness(0, 0, 0, 2.5), Padding = new Thickness(0, 0, 0, 10) });
            headerGroup.Rows.Add(headerRow);
            headerTable.RowGroups.Add(headerGroup);
            doc.Blocks.Add(headerTable);

            // 2. Tiêu đề chính giữa
            var title = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 14) };
            title.Inlines.Add(new Bold(new Run($"PHIẾU BÁO TIỀN TRỌ KỲ THÁNG {hd.Thang:D2}/{hd.Nam}\n")) { FontSize = 19, Foreground = Mau("#0F172A") });
            title.Inlines.Add(new Run("Chi tiết các khoản tiền thuê phòng, điện nước và dịch vụ phát sinh") { FontSize = 11.5, Foreground = Mau("#64748B") });
            doc.Blocks.Add(title);

            // 3. Khung thông tin Phòng & Khách thuê
            var infoBox = new Table { CellSpacing = 0, Background = Mau("#F8FAFC"), BorderBrush = Mau("#CBD5E1"), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 16) };
            infoBox.Columns.Add(new TableColumn { Width = new GridLength(300) });
            infoBox.Columns.Add(new TableColumn { Width = new GridLength(300) });
            var infoGroup = new TableRowGroup();
            var infoRow = new TableRow();

            var infoLeft = new Paragraph { Margin = new Thickness(12, 10, 12, 10), LineHeight = 20 };
            infoLeft.Inlines.Add(new Run("Phòng thuê: ") { Foreground = Mau("#64748B") });
            infoLeft.Inlines.Add(new Bold(new Run($"{hd.Phong?.TenPhong} ({hd.Phong?.MaPhong})\n")) { Foreground = Mau("#1E3A8A") });
            infoLeft.Inlines.Add(new Run("Mã hợp đồng: ") { Foreground = Mau("#64748B") });
            infoLeft.Inlines.Add(new Run($"{hd.HopDong?.MaHopDong ?? "---"}"));

            var infoRight = new Paragraph { Margin = new Thickness(12, 10, 12, 10), LineHeight = 20 };
            infoRight.Inlines.Add(new Run("Khách đại diện: ") { Foreground = Mau("#64748B") });
            infoRight.Inlines.Add(new Bold(new Run($"{hd.HopDong?.KhachDaiDien?.HoTen ?? "---"}\n")));
            infoRight.Inlines.Add(new Run("Số điện thoại: ") { Foreground = Mau("#64748B") });
            infoRight.Inlines.Add(new Run($"{hd.HopDong?.KhachDaiDien?.SoDienThoai ?? "---"}"));

            infoRow.Cells.Add(new TableCell(infoLeft));
            infoRow.Cells.Add(new TableCell(infoRight));
            infoGroup.Rows.Add(infoRow);
            infoBox.RowGroups.Add(infoGroup);
            doc.Blocks.Add(infoBox);

            // 4. Bảng chi tiết các khoản thu (3 cột: STT | Hạng mục & Diễn giải | Thành tiền)
            var table = new Table { CellSpacing = 0, BorderBrush = Mau("#CBD5E1"), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 14) };
            table.Columns.Add(new TableColumn { Width = new GridLength(46) });
            table.Columns.Add(new TableColumn { Width = new GridLength(384) });
            table.Columns.Add(new TableColumn { Width = new GridLength(170) });

            var rowGroup = new TableRowGroup();

            // Dòng tiêu đề bảng màu xanh Navy
            var thRow = new TableRow { Background = Mau("#1E3A8A") };
            thRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("STT"))) { Foreground = Brushes.White, TextAlignment = TextAlignment.Center, Margin = new Thickness(6, 9, 6, 9) }));
            thRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("HẠNG MỤC & DIỄN GIẢI"))) { Foreground = Brushes.White, Margin = new Thickness(10, 9, 10, 9) }));
            thRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("THÀNH TIỀN (VNĐ)"))) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 9, 10, 9) }));
            rowGroup.Rows.Add(thRow);

            void ThemDongChiTiet(string stt, string tieuDe, string dienGiai, decimal soTien, bool nenXam)
            {
                var row = new TableRow { Background = nenXam ? Mau("#F8FAFC") : Brushes.White };

                var pStt = new Paragraph(new Run(stt)) { TextAlignment = TextAlignment.Center, Margin = new Thickness(6, 8, 6, 8), Foreground = Mau("#475569") };

                var pNoiDung = new Paragraph { Margin = new Thickness(10, 7, 10, 7) };
                pNoiDung.Inlines.Add(new Bold(new Run(tieuDe)) { Foreground = Mau("#0F172A") });
                if (!string.IsNullOrWhiteSpace(dienGiai))
                {
                    pNoiDung.Inlines.Add(new LineBreak());
                    pNoiDung.Inlines.Add(new Run(dienGiai) { FontSize = 11, Foreground = Mau("#64748B") });
                }

                var pTien = new Paragraph(new Bold(new Run($"{soTien:N0} đ"))) { TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 8, 10, 8), Foreground = Mau("#0F172A") };

                var borderColor = Mau("#E2E8F0");
                row.Cells.Add(new TableCell(pStt) { BorderBrush = borderColor, BorderThickness = new Thickness(0, 0, 1, 1) });
                row.Cells.Add(new TableCell(pNoiDung) { BorderBrush = borderColor, BorderThickness = new Thickness(0, 0, 1, 1) });
                row.Cells.Add(new TableCell(pTien) { BorderBrush = borderColor, BorderThickness = new Thickness(0, 0, 0, 1) });
                rowGroup.Rows.Add(row);
            }

            ThemDongChiTiet("1", "Tiền thuê phòng", $"Kỳ thuê tháng {hd.Thang:D2}/{hd.Nam}", hd.TienPhong, false);
            ThemDongChiTiet("2", "Tiền điện sinh hoạt", chiTietDien, hd.TienDien, true);
            ThemDongChiTiet("3", "Tiền nước sinh hoạt", chiTietNuoc, hd.TienNuoc, false);
            ThemDongChiTiet("4", "Phí dịch vụ (Wifi, rác, gửi xe...)", "Theo biểu phí dịch vụ nhà trọ", hd.TienDichVu, true);
            ThemDongChiTiet("5", "Nợ cũ kỳ trước chuyển sang", "Số dư công nợ gốc chưa thanh toán các kỳ trước", hd.NoCu, false);

            // Các dòng tổng kết
            void ThemDongTongKet(string nhan, decimal soTien, SolidColorBrush mauNen, SolidColorBrush mauChu, double coChu = 13)
            {
                var row = new TableRow { Background = mauNen };
                var cellNhan = new TableCell(new Paragraph(new Bold(new Run(nhan))) { TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 9, 10, 9), Foreground = mauChu, FontSize = coChu })
                {
                    ColumnSpan = 2,
                    BorderBrush = Mau("#CBD5E1"),
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                var cellTien = new TableCell(new Paragraph(new Bold(new Run($"{soTien:N0} VNĐ"))) { TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 9, 10, 9), Foreground = mauChu, FontSize = coChu })
                {
                    BorderBrush = Mau("#CBD5E1"),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                row.Cells.Add(cellNhan);
                row.Cells.Add(cellTien);
                rowGroup.Rows.Add(row);
            }

            ThemDongTongKet("TỔNG CỘNG PHẢI THU", hd.TongCong, Mau("#EFF6FF"), Mau("#1E3A8A"), 13.5);
            ThemDongTongKet("Đã thanh toán", hd.SoTienDaDong, Brushes.White, Mau("#047857"), 12.5);
            ThemDongTongKet("CÒN PHẢI THANH TOÁN", hd.ConNo, hd.ConNo > 0 ? Mau("#FEF2F2") : Mau("#ECFDF5"), hd.ConNo > 0 ? Mau("#DC2626") : Mau("#047857"), 14);

            table.RowGroups.Add(rowGroup);
            doc.Blocks.Add(table);

            // 5. Lịch sử các đợt thanh toán (nếu khách đã đóng tiền)
            var lichSu = _db.ThanhToans.AsNoTracking()
                .Where(t => t.HoaDonId == hd.Id)
                .OrderBy(t => t.NgayThanhToan)
                .ToList();

            if (lichSu.Any())
            {
                var pLsTitle = new Paragraph(new Bold(new Run("Lịch sử thanh toán đã ghi nhận:")))
                {
                    FontSize = 11.5,
                    Foreground = Mau("#334155"),
                    Margin = new Thickness(0, 4, 0, 4)
                };
                doc.Blocks.Add(pLsTitle);

                var pLsList = new Paragraph { Margin = new Thickness(8, 0, 0, 10), FontSize = 11.5, Foreground = Mau("#475569"), LineHeight = 18 };
                foreach (var tt in lichSu)
                {
                    pLsList.Inlines.Add(new Run($"• {tt.NgayThanhToan:dd/MM/yyyy HH:mm} — Thu {tt.SoTienThu:N0} VNĐ ({tt.PhuongThuc}) — {tt.GhiChu}\n"));
                }
                doc.Blocks.Add(pLsList);
            }

            // 6. Lưu ý & Khu vực ký tên 2 bên
            var note = new Paragraph(new Run("Quý khách vui lòng kiểm tra kỹ chỉ số điện nước và thanh toán đúng hạn (từ ngày 01 đến ngày 05 hàng tháng). Xin chân thành cảm ơn!"))
            {
                FontStyle = FontStyles.Italic,
                FontSize = 11.5,
                Foreground = Mau("#475569"),
                Margin = new Thickness(0, 6, 0, 22)
            };
            doc.Blocks.Add(note);

            var signTable = new Table { CellSpacing = 0 };
            signTable.Columns.Add(new TableColumn { Width = new GridLength(300) });
            signTable.Columns.Add(new TableColumn { Width = new GridLength(300) });
            var signGroup = new TableRowGroup();
            var signRow = new TableRow();

            var pKhachKy = new Paragraph { TextAlignment = TextAlignment.Center };
            pKhachKy.Inlines.Add(new Bold(new Run("KHÁCH THUÊ PHÒNG\n")) { FontSize = 12 });
            pKhachKy.Inlines.Add(new Run("(Ký và ghi rõ họ tên)\n\n\n\n") { FontSize = 11, FontStyle = FontStyles.Italic, Foreground = Mau("#64748B") });
            pKhachKy.Inlines.Add(new Bold(new Run(hd.HopDong?.KhachDaiDien?.HoTen ?? "")));

            var pChuKy = new Paragraph { TextAlignment = TextAlignment.Center };
            pChuKy.Inlines.Add(new Bold(new Run("NGƯỜI LẬP PHIẾU / CHỦ TRỌ\n")) { FontSize = 12 });
            pChuKy.Inlines.Add(new Run("(Ký và ghi rõ họ tên)\n\n\n\n") { FontSize = 11, FontStyle = FontStyles.Italic, Foreground = Mau("#64748B") });
            pChuKy.Inlines.Add(new Bold(new Run(tenChuTro)));

            signRow.Cells.Add(new TableCell(pKhachKy));
            signRow.Cells.Add(new TableCell(pChuKy));
            signGroup.Rows.Add(signRow);
            signTable.RowGroups.Add(signGroup);
            doc.Blocks.Add(signTable);

            var previewWin = new Views.PreviewHoaDonWindow(doc, hd.MaHoaDon);
            previewWin.ShowDialog();
        }

        [RelayCommand]
        private void XoaHoaDon()
        {
            if (HoaDonDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HoaDonDangChon.TrangThai == 2)
            {
                MessageBox.Show("Hóa đơn đã thanh toán đủ nên đã bị khóa, không thể xóa!", "Đã khóa", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (HoaDonDangChon.SoTienDaDong > 0)
            {
                MessageBox.Show("Hóa đơn đã phát sinh giao dịch thu tiền, không thể xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa hóa đơn {HoaDonDangChon.MaHoaDon} (Phòng {HoaDonDangChon.Phong?.MaPhong})?",
                "Xác nhận xóa hóa đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.HoaDons.Remove(HoaDonDangChon);
                _db.SaveChanges();
                TaiDanhSachHoaDon();
                MessageBox.Show("Đã xóa hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}