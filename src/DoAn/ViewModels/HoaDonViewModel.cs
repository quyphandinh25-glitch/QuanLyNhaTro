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
    public partial class HoaDonViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public List<int> DanhSachThang { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> DanhSachNam { get; } = new() { 2025, 2026, 2027 };

        [ObservableProperty] private int _thangChon = DateTime.Now.Month;
        [ObservableProperty] private int _namChon = DateTime.Now.Year;

        [ObservableProperty] private ObservableCollection<HoaDon> _danhSachHoaDon = new();
        [ObservableProperty] private HoaDon? _hoaDonDangChon;

        // Thống kê nhanh của kỳ hóa đơn đang chọn
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

        public void TaiDanhSachHoaDon()
        {
            var list = _db.HoaDons
                .Include(h => h.Phong)
                .Include(h => h.HopDong).ThenInclude(hd => hd!.KhachDaiDien)
                .Where(h => h.Thang == ThangChon && h.Nam == NamChon)
                .OrderBy(h => h.Phong!.MaPhong)
                .ToList();

            DanhSachHoaDon = new ObservableCollection<HoaDon>(list);
            HoaDonDangChon = DanhSachHoaDon.FirstOrDefault();

            TongPhaiThuThang = list.Sum(x => x.TongCong);
            TongDaThuThang = list.Sum(x => x.SoTienDaDong);
            TongConNoThang = list.Sum(x => x.ConNo);
        }

        // NGHIỆP VỤ TRỌNG TÂM MỤC 2: TỰ ĐỘNG TÍNH TIỀN & LẬP HÓA ĐƠN HÀNG LOẠT
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

            int soHoaDonTaoMoi = 0;
            int soHoaDonCapNhat = 0;

            foreach (var hd in hopDongHieuLuc)
            {
                // 1. Tính tiền phòng (Xử lý quy tắc vào ở giữa tháng: từ sau ngày 5 của tháng lập hóa đơn)
                decimal tienPhong = hd.GiaThueChot;
                if (hd.NgayBatDau.Year == NamChon && hd.NgayBatDau.Month == ThangChon && hd.NgayBatDau.Day > 5)
                {
                    int soNgayO = Math.Max(1, 30 - hd.NgayBatDau.Day + 1);
                    tienPhong = Math.Round((hd.GiaThueChot / 30m * soNgayO) / 1000m) * 1000m;
                }

                // 2. Tính tiền Điện & Nước từ bảng ChiSoDienNuoc của tháng đó
                var chiSo = _db.ChiSoDienNuocs
                    .FirstOrDefault(c => c.PhongId == hd.PhongId && c.Thang == ThangChon && c.Nam == NamChon);

                int soDien = chiSo != null ? Math.Max(0, chiSo.ChiSoDienMoi - chiSo.ChiSoDienCu) : 0;
                int soNuoc = chiSo != null ? Math.Max(0, chiSo.ChiSoNuocMoi - chiSo.ChiSoNuocCu) : 0;

                decimal tienDien = soDien * giaDien;
                decimal tienNuoc = soNuoc * giaNuoc;

                // 3. Tính tiền Dịch vụ phụ (0: Theo phòng, 1: Theo đầu người, 2: Theo số xe)
                decimal tienDichVu = 0;
                foreach (var dv in cacDichVu)
                {
                    if (dv.CachTinh == 0) tienDichVu += dv.DonGia;
                    else if (dv.CachTinh == 1) tienDichVu += dv.DonGia * hd.SoNguoiO;
                    else if (dv.CachTinh == 2) tienDichVu += dv.DonGia * hd.SoLuongXe;
                }

                // 4. Tính Nợ cũ cộng dồn từ các hóa đơn kỳ trước chưa thanh toán đủ
                decimal noCu = _db.HoaDons
                    .Where(b => b.HopDongId == hd.Id && !(b.Thang == ThangChon && b.Nam == NamChon) && b.TrangThai != 2)
                    .Sum(b => b.ConNo);

                decimal tongCong = tienPhong + tienDien + tienNuoc + tienDichVu + noCu;

                // Kiểm tra xem hóa đơn tháng này của phòng đó đã tạo chưa
                var hoaDonHienTai = _db.HoaDons
                    .FirstOrDefault(b => b.PhongId == hd.PhongId && b.Thang == ThangChon && b.Nam == NamChon);

                if (hoaDonHienTai == null)
                {
                    string maBill = $"BILL-{ThangChon:D2}{NamChon}-{hd.Phong?.MaPhong}";
                    hoaDonHienTai = new HoaDon
                    {
                        MaHoaDon = maBill,
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
                        TrangThai = 0, // 0: Chưa thanh toán
                        NgayLap = DateTime.Now
                    };
                    _db.HoaDons.Add(hoaDonHienTai);
                    soHoaDonTaoMoi++;
                }
                else if (hoaDonHienTai.TrangThai != 2) // Nếu chưa thanh toán đủ thì cho phép tính lại số liệu mới
                {
                    hoaDonHienTai.TienPhong = tienPhong;
                    hoaDonHienTai.TienDien = tienDien;
                    hoaDonHienTai.TienNuoc = tienNuoc;
                    hoaDonHienTai.TienDichVu = tienDichVu;
                    hoaDonHienTai.NoCu = noCu;
                    hoaDonHienTai.TongCong = tongCong;
                    hoaDonHienTai.ConNo = Math.Max(0, tongCong - hoaDonHienTai.SoTienDaDong);
                    soHoaDonCapNhat++;
                }
            }

            _db.SaveChanges();
            TaiDanhSachHoaDon();

            MessageBox.Show(
                $"⚡ Đã tính toán & phát hành hóa đơn kỳ Tháng {ThangChon}/{NamChon} hoàn tất!\n\n" +
                $"• Tạo mới: {soHoaDonTaoMoi} hóa đơn\n" +
                $"• Cập nhật lại số liệu: {soHoaDonCapNhat} hóa đơn",
                "Lập hóa đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Xóa hóa đơn (nếu lập nhầm và chưa thu tiền)
        [RelayCommand]
        private void XoaHoaDon()
        {
            if (HoaDonDangChon == null) return;
            if (HoaDonDangChon.SoTienDaDong > 0)
            {
                MessageBox.Show("Hóa đơn này đã phát sinh giao dịch thu tiền, không thể xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
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