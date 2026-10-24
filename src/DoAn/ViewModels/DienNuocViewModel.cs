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
    // Class phụ trợ hiển thị từng dòng chốt điện nước trên bảng
    public partial class ChiSoDienNuocItem : ObservableObject
    {
        public ChiSoDienNuoc BanGhiGoc { get; set; } = null!;
        public string MaPhong => BanGhiGoc.Phong?.MaPhong ?? "";
        public string TenPhong => BanGhiGoc.Phong?.TenPhong ?? "";

        [ObservableProperty] private int _chiSoDienCu;
        [ObservableProperty] private int _chiSoDienMoi;
        [ObservableProperty] private int _chiSoNuocCu;
        [ObservableProperty] private int _chiSoNuocMoi;

        public int SoDienTieuThu => Math.Max(0, ChiSoDienMoi - ChiSoDienCu);
        public int SoNuocTieuThu => Math.Max(0, ChiSoNuocMoi - ChiSoNuocCu);

        partial void OnChiSoDienCuChanged(int value) => OnPropertyChanged(nameof(SoDienTieuThu));
        partial void OnChiSoDienMoiChanged(int value) => OnPropertyChanged(nameof(SoDienTieuThu));
        partial void OnChiSoNuocCuChanged(int value) => OnPropertyChanged(nameof(SoNuocTieuThu));
        partial void OnChiSoNuocMoiChanged(int value) => OnPropertyChanged(nameof(SoNuocTieuThu));
    }

    public partial class DienNuocViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public List<int> DanhSachThang { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> DanhSachNam { get; } = new() { 2025, 2026, 2027 };

        [ObservableProperty] private int _thangChon = DateTime.Now.Month;
        [ObservableProperty] private int _namChon = DateTime.Now.Year;

        [ObservableProperty] private ObservableCollection<ChiSoDienNuocItem> _danhSachChiSo = new();
        [ObservableProperty] private ObservableCollection<DichVu> _danhSachDichVu = new();

        // Đơn giá điện nước mặc định
        [ObservableProperty] private decimal _donGiaDien;
        [ObservableProperty] private decimal _donGiaNuoc;

        public DienNuocViewModel(AppDbContext db)
        {
            _db = db;
            TaiCauHinhVaDichVu();
            TaiDanhSachChiSo();
        }

        partial void OnThangChonChanged(int value) => TaiDanhSachChiSo();
        partial void OnNamChonChanged(int value) => TaiDanhSachChiSo();

        private void TaiCauHinhVaDichVu()
        {
            var cauHinh = _db.NguoiDungs.FirstOrDefault();
            if (cauHinh != null)
            {
                DonGiaDien = cauHinh.DonGiaDienMacDinh;
                DonGiaNuoc = cauHinh.DonGiaNuocMacDinh;
            }
            DanhSachDichVu = new ObservableCollection<DichVu>(_db.DichVus.ToList());
        }

        // Tự động khởi tạo bảng chốt số cho các phòng đang thuê trong tháng được chọn
        [RelayCommand]
        public void TaiDanhSachChiSo()
        {
            // Lấy tất cả phòng đang có người thuê (Trạng thái 1: Đang thuê, 2: Sắp hết hạn)
            var phongDangThue = _db.Phongs
                .Where(p => p.TrangThai == 1 || p.TrangThai == 2)
                .OrderBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .ToList();

            // Tính tháng/năm liền kề trước đó để kế thừa Chỉ số cũ
            int thangTruoc = ThangChon == 1 ? 12 : ThangChon - 1;
            int namTruoc = ThangChon == 1 ? NamChon - 1 : NamChon;

            foreach (var p in phongDangThue)
            {
                var banGhiHienTai = _db.ChiSoDienNuocs
                    .FirstOrDefault(c => c.PhongId == p.Id && c.Thang == ThangChon && c.Nam == NamChon);

                if (banGhiHienTai == null)
                {
                    // Tìm chỉ số mới của tháng trước làm chỉ số cũ tháng này
                    var banGhiThangTruoc = _db.ChiSoDienNuocs
                        .FirstOrDefault(c => c.PhongId == p.Id && c.Thang == thangTruoc && c.Nam == namTruoc);

                    int dienCu = banGhiThangTruoc?.ChiSoDienMoi ?? 100;
                    int nuocCu = banGhiThangTruoc?.ChiSoNuocMoi ?? 20;

                    banGhiHienTai = new ChiSoDienNuoc
                    {
                        PhongId = p.Id,
                        Thang = ThangChon,
                        Nam = NamChon,
                        ChiSoDienCu = dienCu,
                        ChiSoDienMoi = dienCu, // Mặc định bằng chỉ số cũ chờ người dùng nhập
                        ChiSoNuocCu = nuocCu,
                        ChiSoNuocMoi = nuocCu,
                        NgayChot = DateTime.Now
                    };
                    _db.ChiSoDienNuocs.Add(banGhiHienTai);
                }
            }
            _db.SaveChanges();

            // Đọc danh sách lên giao diện
            var duLieuThang = _db.ChiSoDienNuocs
                .Include(c => c.Phong)
                .Where(c => c.Thang == ThangChon && c.Nam == NamChon)
                .OrderBy(c => c.Phong!.MaPhong)
                .ToList();

            var items = duLieuThang.Select(c => new ChiSoDienNuocItem
            {
                BanGhiGoc = c,
                ChiSoDienCu = c.ChiSoDienCu,
                ChiSoDienMoi = c.ChiSoDienMoi,
                ChiSoNuocCu = c.ChiSoNuocCu,
                ChiSoNuocMoi = c.ChiSoNuocMoi
            }).ToList();

            DanhSachChiSo = new ObservableCollection<ChiSoDienNuocItem>(items);
        }

        // Lưu toàn bộ chỉ số điện nước vừa nhập trên bảng
        [RelayCommand]
        private void LuuChiSoDienNuoc()
        {
            foreach (var item in DanhSachChiSo)
            {
                // Ràng buộc 1: Chỉ số mới phải >= Chỉ số cũ
                if (item.ChiSoDienMoi < item.ChiSoDienCu)
                {
                    MessageBox.Show($"Phòng {item.MaPhong}: Chỉ số Điện mới ({item.ChiSoDienMoi}) không được nhỏ hơn chỉ số cũ ({item.ChiSoDienCu})!",
                        "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (item.ChiSoNuocMoi < item.ChiSoNuocCu)
                {
                    MessageBox.Show($"Phòng {item.MaPhong}: Chỉ số Nước mới ({item.ChiSoNuocMoi}) không được nhỏ hơn chỉ số cũ ({item.ChiSoNuocCu})!",
                        "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ràng buộc 2: Cảnh báo nếu tiêu thụ cao bất thường (> 500 kWh điện hoặc > 50 m3 nước)
                if (item.SoDienTieuThu > 500 || item.SoNuocTieuThu > 50)
                {
                    var confirm = MessageBox.Show(
                        $"⚠️ Cảnh báo bất thường tại phòng {item.MaPhong}:\n" +
                        $"• Điện tiêu thụ: {item.SoDienTieuThu} kWh\n" +
                        $"• Nước tiêu thụ: {item.SoNuocTieuThu} m³\n\n" +
                        $"Số liệu này cao bất thường, bạn có chắc chắn đã nhập đúng không?",
                        "Cảnh báo tiêu thụ cao", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (confirm == MessageBoxResult.No) return;
                }

                // Cập nhật vào Entity gốc
                item.BanGhiGoc.ChiSoDienCu = item.ChiSoDienCu;
                item.BanGhiGoc.ChiSoDienMoi = item.ChiSoDienMoi;
                item.BanGhiGoc.ChiSoNuocCu = item.ChiSoNuocCu;
                item.BanGhiGoc.ChiSoNuocMoi = item.ChiSoNuocMoi;
                item.BanGhiGoc.NgayChot = DateTime.Now;
            }

            _db.SaveChanges();
            MessageBox.Show($"Đã lưu bảng chốt chỉ số Điện & Nước kỳ Tháng {ThangChon}/{NamChon} thành công!",
                "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Lưu cập nhật đơn giá Điện, Nước và các Dịch vụ phụ
        [RelayCommand]
        private void LuuDonGiaDichVu()
        {
            var cauHinh = _db.NguoiDungs.FirstOrDefault();
            if (cauHinh != null)
            {
                cauHinh.DonGiaDienMacDinh = DonGiaDien;
                cauHinh.DonGiaNuocMacDinh = DonGiaNuoc;
            }

            _db.SaveChanges();
            MessageBox.Show("Đã cập nhật bảng đơn giá Điện, Nước và Dịch vụ thành công!",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}