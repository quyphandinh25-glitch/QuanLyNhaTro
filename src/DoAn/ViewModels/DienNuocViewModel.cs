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
        public Action? OnChiSoThayDoi { get; set; }

        public string MaPhong => BanGhiGoc.Phong?.MaPhong ?? "";
        public string TenPhong => BanGhiGoc.Phong?.TenPhong ?? "";

        // Giữ nguyên các [ObservableProperty] gốc để tránh xung đột Source Generator
        [ObservableProperty] private int _chiSoDienCu;
        [ObservableProperty] private int _chiSoDienMoi;
        [ObservableProperty] private int _chiSoNuocCu;
        [ObservableProperty] private int _chiSoNuocMoi;

        public int SoDienTieuThu => Math.Max(0, ChiSoDienMoi - ChiSoDienCu);
        public int SoNuocTieuThu => Math.Max(0, ChiSoNuocMoi - ChiSoNuocCu);

        // Thuộc tính cảnh báo trực quan ngay khi nhập sai chỉ số trên bảng
        public bool DienKhongHopLe => ChiSoDienCu < 0 || ChiSoDienMoi < 0 || ChiSoDienMoi < ChiSoDienCu;
        public bool NuocKhongHopLe => ChiSoNuocCu < 0 || ChiSoNuocMoi < 0 || ChiSoNuocMoi < ChiSoNuocCu;

        public string DienTieuThuText => DienKhongHopLe ? "⚠️ Lỗi số!" : $"{SoDienTieuThu} kWh";
        public string NuocTieuThuText => NuocKhongHopLe ? "⚠️ Lỗi số!" : $"{SoNuocTieuThu} m³";

        public string MauNenDien => DienKhongHopLe ? "#FEE2E2" : "#EFF6FF";
        public string MauChuDien => DienKhongHopLe ? "#B91C1C" : "#1D4ED8";

        public string MauNenNuoc => NuocKhongHopLe ? "#FEE2E2" : "#ECFDF5";
        public string MauChuNuoc => NuocKhongHopLe ? "#B91C1C" : "#047857";

        private void ThongBaoCapNhatDien()
        {
            OnPropertyChanged(nameof(SoDienTieuThu));
            OnPropertyChanged(nameof(DienKhongHopLe));
            OnPropertyChanged(nameof(DienTieuThuText));
            OnPropertyChanged(nameof(MauNenDien));
            OnPropertyChanged(nameof(MauChuDien));
            OnChiSoThayDoi?.Invoke();
        }

        private void ThongBaoCapNhatNuoc()
        {
            OnPropertyChanged(nameof(SoNuocTieuThu));
            OnPropertyChanged(nameof(NuocKhongHopLe));
            OnPropertyChanged(nameof(NuocTieuThuText));
            OnPropertyChanged(nameof(MauNenNuoc));
            OnPropertyChanged(nameof(MauChuNuoc));
            OnChiSoThayDoi?.Invoke();
        }

        partial void OnChiSoDienCuChanged(int value) => ThongBaoCapNhatDien();
        partial void OnChiSoDienMoiChanged(int value) => ThongBaoCapNhatDien();
        partial void OnChiSoNuocCuChanged(int value) => ThongBaoCapNhatNuoc();
        partial void OnChiSoNuocMoiChanged(int value) => ThongBaoCapNhatNuoc();
    }

    public partial class DienNuocViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private List<ChiSoDienNuocItem> _tatCaChiSoTrongKy = new();

        public List<int> DanhSachThang { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> DanhSachNam { get; } = new() { 2025, 2026, 2027, 2028 };

        // Giữ nguyên các [ObservableProperty] gốc
        [ObservableProperty] private int _thangChon = DateTime.Now.Month;
        [ObservableProperty] private int _namChon = DateTime.Now.Year;

        [ObservableProperty] private ObservableCollection<ChiSoDienNuocItem> _danhSachChiSo = new();
        [ObservableProperty] private ObservableCollection<DichVu> _danhSachDichVu = new();

        [ObservableProperty] private decimal _donGiaDien;
        [ObservableProperty] private decimal _donGiaNuoc;

        // Các thuộc tính mới khai báo tường minh để không bị CS0103
        private string _tuKhoaTimKiem = string.Empty;
        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set
            {
                if (SetProperty(ref _tuKhoaTimKiem, value))
                    ApDungBoLoc();
            }
        }

        private string _tongPhongText = "0 phòng";
        public string TongPhongText
        {
            get => _tongPhongText;
            set => SetProperty(ref _tongPhongText, value);
        }

        private string _tongDienText = "⚡ Tổng điện: 0 kWh";
        public string TongDienText
        {
            get => _tongDienText;
            set => SetProperty(ref _tongDienText, value);
        }

        private string _tongNuocText = "💧 Tổng nước: 0 m³";
        public string TongNuocText
        {
            get => _tongNuocText;
            set => SetProperty(ref _tongNuocText, value);
        }

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

        // Tự động khởi tạo bảng chốt số cho các phòng có hợp đồng hiệu lực trong kỳ (giữ chuẩn logic đã fix)
        [RelayCommand]
        public void TaiDanhSachChiSo()
        {
            DateTime ngayDauThang = new DateTime(NamChon, ThangChon, 1);
            DateTime ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            // 1. Lấy danh sách ID các phòng có Hợp đồng đang hiệu lực trong tháng/năm được chọn
            var phongDangThueIds = _db.HopDongs
                .Where(h => h.TrangThai == 0
                         && h.NgayBatDau <= ngayCuoiThang
                         && h.NgayKetThuc >= ngayDauThang)
                .Select(h => h.PhongId)
                .Distinct()
                .ToList();

            // 2. Tự động khởi tạo bản ghi chỉ số điện nước nếu phòng chưa có trong tháng này
            foreach (var phongId in phongDangThueIds)
            {
                bool daCo = _db.ChiSoDienNuocs.Any(c => c.PhongId == phongId && c.Thang == ThangChon && c.Nam == NamChon);
                if (!daCo)
                {
                    var chiSoKyTruoc = _db.ChiSoDienNuocs
                        .Where(c => c.PhongId == phongId && (c.Nam < NamChon || (c.Nam == NamChon && c.Thang < ThangChon)))
                        .OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang)
                        .FirstOrDefault();

                    int dienCu = chiSoKyTruoc?.ChiSoDienMoi ?? 100;
                    int nuocCu = chiSoKyTruoc?.ChiSoNuocMoi ?? 20;

                    _db.ChiSoDienNuocs.Add(new ChiSoDienNuoc
                    {
                        PhongId = phongId,
                        Thang = ThangChon,
                        Nam = NamChon,
                        ChiSoDienCu = dienCu,
                        ChiSoDienMoi = dienCu,
                        ChiSoNuocCu = nuocCu,
                        ChiSoNuocMoi = nuocCu,
                        NgayChot = DateTime.Now
                    });
                }
            }
            _db.SaveChanges();

            // 3. Chỉ tải lên bảng chỉ số của đúng các phòng có hợp đồng trong kỳ
            var danhSach = _db.ChiSoDienNuocs
                .Include(c => c.Phong)
                .Where(c => c.Thang == ThangChon
                         && c.Nam == NamChon
                         && phongDangThueIds.Contains(c.PhongId))
                .OrderBy(c => c.Phong!.MaPhong)
                .ToList();

            _tatCaChiSoTrongKy = danhSach.Select(c => new ChiSoDienNuocItem
            {
                BanGhiGoc = c,
                ChiSoDienCu = c.ChiSoDienCu,
                ChiSoDienMoi = c.ChiSoDienMoi,
                ChiSoNuocCu = c.ChiSoNuocCu,
                ChiSoNuocMoi = c.ChiSoNuocMoi,
                OnChiSoThayDoi = CapNhatTongTieuThu
            }).ToList();

            ApDungBoLoc();
        }

        private void ApDungBoLoc()
        {
            IEnumerable<ChiSoDienNuocItem> ketQua = _tatCaChiSoTrongKy;

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQua = ketQua.Where(x =>
                    x.MaPhong.ToLower().Contains(kw) ||
                    x.TenPhong.ToLower().Contains(kw));
            }

            var dsHienThi = ketQua.ToList();
            DanhSachChiSo = new ObservableCollection<ChiSoDienNuocItem>(dsHienThi);
            TongPhongText = $"Hiển thị {dsHienThi.Count} / {_tatCaChiSoTrongKy.Count} phòng";
            CapNhatTongTieuThu();
        }

        private void CapNhatTongTieuThu()
        {
            int tongDien = DanhSachChiSo.Sum(x => x.SoDienTieuThu);
            int tongNuoc = DanhSachChiSo.Sum(x => x.SoNuocTieuThu);
            TongDienText = $"⚡ Tổng điện: {tongDien:N0} kWh";
            TongNuocText = $"💧 Tổng nước: {tongNuoc:N0} m³";
        }

        // Lưu toàn bộ chỉ số điện nước vừa nhập trên bảng (kiểm tra 2 bước an toàn)
        [RelayCommand]
        private void LuuChiSoDienNuoc()
        {
            if (_tatCaChiSoTrongKy.Count == 0)
            {
                MessageBox.Show($"Không có phòng nào đang lưu trú trong kỳ Tháng {ThangChon}/{NamChon} để chốt chỉ số!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Bước 1: Kiểm tra hợp lệ toàn bộ các phòng trước khi ghi vào CSDL
            foreach (var item in _tatCaChiSoTrongKy)
            {
                if (item.ChiSoDienCu < 0 || item.ChiSoDienMoi < 0 || item.ChiSoNuocCu < 0 || item.ChiSoNuocMoi < 0)
                {
                    MessageBox.Show($"Phòng {item.MaPhong}: Chỉ số Điện và Nước không được là số âm!",
                        "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ràng buộc 1: Chỉ số Điện mới < cũ (hỗ trợ xác nhận thay đồng hồ mới)
                if (item.ChiSoDienMoi < item.ChiSoDienCu)
                {
                    var xacNhanThayDien = MessageBox.Show(
                        $"Phòng {item.MaPhong}: Chỉ số Điện mới ({item.ChiSoDienMoi}) nhỏ hơn chỉ số cũ ({item.ChiSoDienCu})!\n\n" +
                        $"Phòng này có vừa THAY ĐỒNG HỒ ĐIỆN MỚI không?\n" +
                        $"• Chọn YES: Tự động đặt Chỉ số Điện cũ về 0 (tính tiêu thụ {item.ChiSoDienMoi} kWh).\n" +
                        $"• Chọn NO: Hủy lưu để bạn kiểm tra và sửa lại chỉ số.",
                        "Cảnh báo chỉ số Điện", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (xacNhanThayDien == MessageBoxResult.Yes)
                    {
                        item.ChiSoDienCu = 0;
                    }
                    else
                    {
                        return;
                    }
                }

                // Ràng buộc 2: Chỉ số Nước mới < cũ (hỗ trợ xác nhận thay đồng hồ mới)
                if (item.ChiSoNuocMoi < item.ChiSoNuocCu)
                {
                    var xacNhanThayNuoc = MessageBox.Show(
                        $"Phòng {item.MaPhong}: Chỉ số Nước mới ({item.ChiSoNuocMoi}) nhỏ hơn chỉ số cũ ({item.ChiSoNuocCu})!\n\n" +
                        $"Phòng này có vừa THAY ĐỒNG HỒ NƯỚC MỚI không?\n" +
                        $"• Chọn YES: Tự động đặt Chỉ số Nước cũ về 0 (tính tiêu thụ {item.ChiSoNuocMoi} m³).\n" +
                        $"• Chọn NO: Hủy lưu để bạn kiểm tra và sửa lại chỉ số.",
                        "Cảnh báo chỉ số Nước", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (xacNhanThayNuoc == MessageBoxResult.Yes)
                    {
                        item.ChiSoNuocCu = 0;
                    }
                    else
                    {
                        return;
                    }
                }

                // Ràng buộc 3: Cảnh báo nếu tiêu thụ cao bất thường (> 500 kWh điện hoặc > 50 m³ nước)
                if (item.SoDienTieuThu > 500 || item.SoNuocTieuThu > 50)
                {
                    var confirm = MessageBox.Show(
                        $"⚠️ Cảnh báo tiêu thụ cao bất thường tại phòng {item.MaPhong}:\n" +
                        $"• Điện tiêu thụ: {item.SoDienTieuThu:N0} kWh\n" +
                        $"• Nước tiêu thụ: {item.SoNuocTieuThu:N0} m³\n\n" +
                        $"Bạn có chắc chắn đã nhập đúng chỉ số này không?",
                        "Xác nhận tiêu thụ cao", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (confirm == MessageBoxResult.No) return;
                }
            }

            // Bước 2: Tất cả hợp lệ -> Ghi đồng loạt xuống Entity & SQLite
            foreach (var item in _tatCaChiSoTrongKy)
            {
                item.BanGhiGoc.ChiSoDienCu = item.ChiSoDienCu;
                item.BanGhiGoc.ChiSoDienMoi = item.ChiSoDienMoi;
                item.BanGhiGoc.ChiSoNuocCu = item.ChiSoNuocCu;
                item.BanGhiGoc.ChiSoNuocMoi = item.ChiSoNuocMoi;
                item.BanGhiGoc.NgayChot = DateTime.Now;
            }

            _db.SaveChanges();
            CapNhatTongTieuThu();
            MessageBox.Show($"Đã lưu bảng chốt chỉ số Điện & Nước kỳ Tháng {ThangChon}/{NamChon} cho {_tatCaChiSoTrongKy.Count} phòng thành công!",
                "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Lưu cập nhật đơn giá Điện, Nước và các Dịch vụ phụ
        [RelayCommand]
        private void LuuDonGiaDichVu()
        {
            if (DonGiaDien <= 0 || DonGiaNuoc <= 0)
            {
                MessageBox.Show("Đơn giá Điện và đơn giá Nước phải lớn hơn 0!", "Đơn giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DanhSachDichVu.Any(d => d.DonGia < 0))
            {
                MessageBox.Show("Đơn giá các dịch vụ phụ không được là số âm!", "Đơn giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cauHinh = _db.NguoiDungs.FirstOrDefault();
            if (cauHinh != null)
            {
                cauHinh.DonGiaDienMacDinh = DonGiaDien;
                cauHinh.DonGiaNuocMacDinh = DonGiaNuoc;
            }

            _db.SaveChanges();
            MessageBox.Show("Đã cập nhật bảng đơn giá Điện, Nước và Dịch vụ thành công!",
                "Cập nhật thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}