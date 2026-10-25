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
        public List<int> DanhSachNam { get; } = new() { 2025, 2026, 2027 };

        // MỤC 4: Bộ lọc theo dõi Công nợ
        public List<string> DanhSachLocCongNo { get; } = new()
        {
            "Tất cả hóa đơn",
            "⚠️ Tất cả đang còn nợ",
            "0 - Chưa thanh toán",
            "1 - Đóng một phần (Còn nợ)",
            "2 - Đã thanh toán đủ"
        };

        public List<string> DanhSachPhuongThuc { get; } = new() { "Chuyển khoản", "Tiền mặt" };

        [ObservableProperty] private int _thangChon = DateTime.Now.Month;
        [ObservableProperty] private int _namChon = DateTime.Now.Year;
        [ObservableProperty] private string _locCongNoChon = "Tất cả hóa đơn";

        [ObservableProperty] private ObservableCollection<HoaDon> _danhSachHoaDon = new();
        [ObservableProperty] private HoaDon? _hoaDonDangChon;

        // MỤC 3: Thuộc tính phục vụ Thu tiền & Lịch sử thanh toán
        [ObservableProperty] private decimal _soTienKhachDong;
        [ObservableProperty] private string _phuongThucChon = "Chuyển khoản";
        [ObservableProperty] private string _ghiChuThanhToan = string.Empty;
        [ObservableProperty] private ObservableCollection<ThanhToan> _lichSuThanhToan = new();

        // Thống kê tổng hợp kỳ
        [ObservableProperty] private decimal _tongPhaiThuThang;
        [ObservableProperty] private decimal _tongDaThuThang;
        [ObservableProperty] private decimal _tongConNoThang;

        public HoaDonViewModel(AppDbContext db)
        {
            _db = db;
            TaiDanhSachHoaDon();
        }

        partial void OnThangChonChanged(int value) => TaiDanhSachHoaDon();
        partial void OnNamChonChanged(int value) => TaiDanhSachHoaDon();
        partial void OnLocCongNoChonChanged(string value) => TaiDanhSachHoaDon();

        // Khi chọn 1 hóa đơn trên bảng -> Tự động gợi ý số tiền còn nợ & tải lịch sử đóng tiền
        partial void OnHoaDonDangChonChanged(HoaDon? value)
        {
            if (value != null)
            {
                SoTienKhachDong = value.ConNo;
                GhiChuThanhToan = string.Empty;

                var lichSu = _db.ThanhToans
                    .Where(t => t.HoaDonId == value.Id)
                    .OrderByDescending(t => t.NgayThanhToan)
                    .ToList();
                LichSuThanhToan = new ObservableCollection<ThanhToan>(lichSu);
            }
            else
            {
                SoTienKhachDong = 0;
                LichSuThanhToan.Clear();
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

            // Áp dụng bộ lọc Công nợ (Mục 4)
            var ketQuaLoc = tatCaTrongKy.AsEnumerable();
            if (LocCongNoChon.StartsWith("⚠️"))
                ketQuaLoc = ketQuaLoc.Where(h => h.ConNo > 0);
            else if (LocCongNoChon.StartsWith("0"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 0);
            else if (LocCongNoChon.StartsWith("1"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 1);
            else if (LocCongNoChon.StartsWith("2"))
                ketQuaLoc = ketQuaLoc.Where(h => h.TrangThai == 2);

            DanhSachHoaDon = new ObservableCollection<HoaDon>(ketQuaLoc);
            HoaDonDangChon = DanhSachHoaDon.FirstOrDefault();
        }

        // MỤC 2: Tự động tính toán & Lập hóa đơn hàng loạt
        [RelayCommand]
        private void LapHoaDonHangLoat()
        {
            var hopDongHieuLuc = _db.HopDongs
                .Include(h => h.Phong)
                .Where(h => h.TrangThai == 0)
                .ToList();

            if (!hopDongHieuLuc.Any())
            {
                MessageBox.Show("Không có hợp đồng nào đang hiệu lực để lập hóa đơn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cauHinh = _db.NguoiDungs.FirstOrDefault();
            decimal giaDien = cauHinh?.DonGiaDienMacDinh ?? 3500;
            decimal giaNuoc = cauHinh?.DonGiaNuocMacDinh ?? 20000;
            var cacDichVu = _db.DichVus.Where(d => d.BatBuoc).ToList();

            int soTaoMoi = 0, soCapNhat = 0;

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

                decimal noCu = _db.HoaDons
                    .Where(b => b.HopDongId == hd.Id && !(b.Thang == ThangChon && b.Nam == NamChon) && b.TrangThai != 2)
                    .Sum(b => b.ConNo);

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
                    soCapNhat++;
                }
            }

            _db.SaveChanges();
            TaiDanhSachHoaDon();
            MessageBox.Show($"Đã lập hóa đơn Tháng {ThangChon}/{NamChon}!\n• Tạo mới: {soTaoMoi}\n• Cập nhật: {soCapNhat}",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // MỤC 3: NGHIỆP VỤ THU TIỀN (THANH TOÁN ĐỦ HOẶC MỘT PHẦN)
        [RelayCommand]
        private void ThuTienHoaDon()
        {
            if (HoaDonDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn cần thu tiền!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HoaDonDangChon.ConNo <= 0 || HoaDonDangChon.TrangThai == 2)
            {
                MessageBox.Show("Hóa đơn này đã được thanh toán đủ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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

            // 2. Cập nhật số tiền đã đóng, công nợ và trạng thái của Hóa đơn
            HoaDonDangChon.SoTienDaDong += SoTienKhachDong;
            HoaDonDangChon.ConNo = HoaDonDangChon.TongCong - HoaDonDangChon.SoTienDaDong;

            if (HoaDonDangChon.ConNo <= 0)
            {
                HoaDonDangChon.ConNo = 0;
                HoaDonDangChon.TrangThai = 2; // 2 = Đã thanh toán đủ
            }
            else
            {
                HoaDonDangChon.TrangThai = 1; // 1 = Thanh toán một phần (Còn nợ)
            }

            _db.SaveChanges();

            int idVuaChon = HoaDonDangChon.Id;
            TaiDanhSachHoaDon();
            HoaDonDangChon = DanhSachHoaDon.FirstOrDefault(h => h.Id == idVuaChon);

            MessageBox.Show("Đã ghi nhận thanh toán và cập nhật công nợ thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // MỤC 5: IN HÓA ĐƠN / XUẤT FILE PDF BẰNG FLOWDOCUMENT CỦA WPF
        // MỤC 5: MỞ CỬA SỔ XEM TRƯỚC HÓA ĐƠN TRƯỚC KHI IN / XUẤT PDF
        [RelayCommand]
        private void InXuatPdfHoaDon()
        {
            if (HoaDonDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn để xem trước và xuất PDF!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tenNhaTro = AuthService.CurrentUser?.TenNhaTro ?? "Nhà Trọ 8386 Thủ Đức";
            string diaChi = AuthService.CurrentUser?.DiaChiNhaTro ?? "TP. Thủ Đức, TP. Hồ Chí Minh";
            string sdtChuTro = AuthService.CurrentUser?.SoDienThoai ?? "0909123456";

            // Tạo tài liệu định dạng phiếu in
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                ColumnWidth = double.PositiveInfinity
            };

            // Tiêu đề Nhà trọ
            var header = new Paragraph();
            header.Inlines.Add(new Bold(new Run(tenNhaTro.ToUpper() + "\n")) { FontSize = 18, Foreground = Brushes.DarkBlue });
            header.Inlines.Add(new Run($"Địa chỉ: {diaChi}  |  SĐT: {sdtChuTro}\n"));
            doc.Blocks.Add(header);

            // Tiêu đề Hóa đơn
            var title = new Paragraph(new Bold(new Run($"PHIẾU BÁO TIỀN TRỌ THÁNG {HoaDonDangChon.Thang}/{HoaDonDangChon.Nam}")))
            {
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 12, 0, 12)
            };
            doc.Blocks.Add(title);

            // Thông tin Phòng & Khách thuê
            var info = new Paragraph();
            info.Inlines.Add(new Run($"• Mã hóa đơn: {HoaDonDangChon.MaHoaDon}\n"));
            info.Inlines.Add(new Bold(new Run($"• Phòng: {HoaDonDangChon.Phong?.TenPhong} ({HoaDonDangChon.Phong?.MaPhong})\n")));
            info.Inlines.Add(new Run($"• Khách đại diện: {HoaDonDangChon.HopDong?.KhachDaiDien?.HoTen ?? "---"}\n"));
            info.Inlines.Add(new Run($"• Ngày lập phiếu: {HoaDonDangChon.NgayLap:dd/MM/yyyy HH:mm}\n"));
            doc.Blocks.Add(info);

            // Bảng chi tiết các khoản thu
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            table.Columns.Add(new TableColumn { Width = new GridLength(320) });
            table.Columns.Add(new TableColumn { Width = new GridLength(180) });

            var rowGroup = new TableRowGroup();
            void ThemDongBang(string tenKhoan, string soTien, bool inDam = false)
            {
                var row = new TableRow();
                var p1 = new Paragraph(inDam ? new Bold(new Run(tenKhoan)) : new Run(tenKhoan)) { Margin = new Thickness(8, 6, 8, 6) };
                var p2 = new Paragraph(inDam ? new Bold(new Run(soTien)) : new Run(soTien)) { Margin = new Thickness(8, 6, 8, 6), TextAlignment = TextAlignment.Right };
                row.Cells.Add(new TableCell(p1) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 1) });
                row.Cells.Add(new TableCell(p2) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
                rowGroup.Rows.Add(row);
            }

            ThemDongBang("HẠNG MỤC THU", "THÀNH TIỀN (VNĐ)", true);
            ThemDongBang("1. Tiền thuê phòng", $"{HoaDonDangChon.TienPhong:N0} đ");
            ThemDongBang("2. Tiền điện tiêu thụ", $"{HoaDonDangChon.TienDien:N0} đ");
            ThemDongBang("3. Tiền nước tiêu thụ", $"{HoaDonDangChon.TienNuoc:N0} đ");
            ThemDongBang("4. Phí dịch vụ (Wifi, rác, gửi xe)", $"{HoaDonDangChon.TienDichVu:N0} đ");
            ThemDongBang("5. Nợ cũ kỳ trước", $"{HoaDonDangChon.NoCu:N0} đ");
            ThemDongBang("TỔNG CỘNG PHẢI THU", $"{HoaDonDangChon.TongCong:N0} VNĐ", true);
            ThemDongBang("Đã thanh toán", $"{HoaDonDangChon.SoTienDaDong:N0} VNĐ");
            ThemDongBang("CÒN NỢ LẠI", $"{HoaDonDangChon.ConNo:N0} VNĐ", true);

            table.RowGroups.Add(rowGroup);
            doc.Blocks.Add(table);

            // Chữ ký cuối trang
            var footer = new Paragraph(new Run("\nQuý khách vui lòng thanh toán đúng hạn. Xin cảm ơn!\n\nNgười lập phiếu: Chủ trọ Quản lý"))
            {
                Margin = new Thickness(0, 20, 0, 0),
                FontStyle = FontStyles.Italic
            };
            doc.Blocks.Add(footer);

            // Mở thẳng cửa sổ Xem trước Hóa đơn
            var previewWin = new Views.PreviewHoaDonWindow(doc, HoaDonDangChon.MaHoaDon);
            previewWin.ShowDialog();
        }

        [RelayCommand]
        private void XoaHoaDon()
        {
            if (HoaDonDangChon == null) return;
            if (HoaDonDangChon.SoTienDaDong > 0)
            {
                MessageBox.Show("Hóa đơn đã có giao dịch thu tiền, không thể xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Xóa hóa đơn {HoaDonDangChon.MaHoaDon}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.HoaDons.Remove(HoaDonDangChon);
                _db.SaveChanges();
                TaiDanhSachHoaDon();
            }
        }
    }
}