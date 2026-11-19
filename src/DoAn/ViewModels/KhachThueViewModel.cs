using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using DoAn.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAn.ViewModels
{
    // Class hiển thị từng dòng Khách thuê trên DataGrid kèm Phòng đang ở & Badge trạng thái
    public class KhachThueHienThiItem
    {
        public KhachThue KhachGoc { get; set; } = null!;
        public int Id => KhachGoc.Id;
        public string HoTen => KhachGoc.HoTen;
        public string CCCD => KhachGoc.CCCD;
        public string SoDienThoai => KhachGoc.SoDienThoai;
        public DateTime NgaySinh => KhachGoc.NgaySinh;
        public string QueQuan => KhachGoc.QueQuan;
        public string NgheNghiep => string.IsNullOrWhiteSpace(KhachGoc.NgheNghiep) ? "---" : KhachGoc.NgheNghiep;

        public bool DangThuePhong { get; set; }
        public string MaPhongDangO { get; set; } = string.Empty;
        public string TrangThaiText => DangThuePhong ? $"Đang ở {MaPhongDangO}" : "Chưa thuê";
        public string MauNenBadge => DangThuePhong ? "#D1FAE5" : "#F1F5F9";
        public string MauChamTron => DangThuePhong ? "#10B981" : "#64748B";
        public string MauChuBadge => DangThuePhong ? "#047857" : "#475569";
    }

    public partial class KhachThueViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        // Giữ nguyên các [ObservableProperty] gốc để tránh xung đột Source Generator
        [ObservableProperty] private ObservableCollection<KhachThue> _danhSachKhach = new();
        [ObservableProperty] private KhachThue? _khachDangChon;
        [ObservableProperty] private string _tuKhoaTimKiem = string.Empty;

        [ObservableProperty] private string _hoTen = string.Empty;
        [ObservableProperty] private string _cCCD = string.Empty;
        [ObservableProperty] private string _soDienThoai = string.Empty;
        [ObservableProperty] private DateTime _ngaySinh = new DateTime(2002, 1, 1);
        [ObservableProperty] private string _queQuan = string.Empty;
        [ObservableProperty] private string _ngheNghiep = string.Empty;

        // Các thuộc tính mới khai báo tường minh để không bị CS0103
        public List<string> DanhSachTrangThaiLoc { get; } = new()
        {
            "Tất cả khách thuê",
            "Đang thuê phòng",
            "Chưa thuê / Đã trả"
        };

        private ObservableCollection<KhachThueHienThiItem> _danhSachKhachHienThi = new();
        public ObservableCollection<KhachThueHienThiItem> DanhSachKhachHienThi
        {
            get => _danhSachKhachHienThi;
            set => SetProperty(ref _danhSachKhachHienThi, value);
        }

        private KhachThueHienThiItem? _khachHienThiDangChon;
        public KhachThueHienThiItem? KhachHienThiDangChon
        {
            get => _khachHienThiDangChon;
            set
            {
                if (SetProperty(ref _khachHienThiDangChon, value))
                {
                    KhachDangChon = value?.KhachGoc;
                }
            }
        }

        private string _trangThaiLocChon = "Tất cả khách thuê";
        public string TrangThaiLocChon
        {
            get => _trangThaiLocChon;
            set
            {
                if (SetProperty(ref _trangThaiLocChon, value))
                    TaiDanhSach();
            }
        }

        private string _tongHienThiText = "0 khách";
        public string TongHienThiText
        {
            get => _tongHienThiText;
            set => SetProperty(ref _tongHienThiText, value);
        }

        public KhachThueViewModel(AppDbContext db)
        {
            _db = db;
            TaiDanhSach();
        }

        partial void OnTuKhoaTimKiemChanged(string value) => TaiDanhSach();

        partial void OnKhachDangChonChanged(KhachThue? value)
        {
            if (value != null)
            {
                HoTen = value.HoTen;
                CCCD = value.CCCD;
                SoDienThoai = value.SoDienThoai;
                NgaySinh = value.NgaySinh;
                QueQuan = value.QueQuan ?? string.Empty;
                NgheNghiep = value.NgheNghiep ?? string.Empty;
            }
        }

        public void TaiDanhSach()
        {
            var tatCaKhach = _db.KhachThues.OrderBy(k => k.HoTen).ToList();

            // Lấy danh sách hợp đồng đang hiệu lực để đối chiếu phòng đang ở của từng khách
            var hopDongHieuLuc = _db.HopDongs
                .AsNoTracking()
                .Include(h => h.Phong)
                .Where(h => h.TrangThai == 0)
                .ToList();

            var dsDayDu = tatCaKhach.Select(k =>
            {
                var dsPhong = hopDongHieuLuc
                    .Where(h => h.KhachDaiDienId == k.Id && h.Phong != null)
                    .Select(h => h.Phong!.MaPhong)
                    .Distinct()
                    .ToList();

                return new KhachThueHienThiItem
                {
                    KhachGoc = k,
                    DangThuePhong = dsPhong.Count > 0,
                    MaPhongDangO = string.Join(", ", dsPhong)
                };
            }).ToList();

            IEnumerable<KhachThueHienThiItem> ketQua = dsDayDu;

            if (TrangThaiLocChon == "Đang thuê phòng")
                ketQua = ketQua.Where(x => x.DangThuePhong);
            else if (TrangThaiLocChon == "Chưa thuê / Đã trả")
                ketQua = ketQua.Where(x => !x.DangThuePhong);

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQua = ketQua.Where(x =>
                    x.HoTen.ToLower().Contains(kw) ||
                    x.CCCD.ToLower().Contains(kw) ||
                    x.SoDienThoai.ToLower().Contains(kw) ||
                    (x.QueQuan ?? "").ToLower().Contains(kw) ||
                    x.MaPhongDangO.ToLower().Contains(kw));
            }

            var dsLoc = ketQua.ToList();
            DanhSachKhach = new ObservableCollection<KhachThue>(dsLoc.Select(x => x.KhachGoc));
            DanhSachKhachHienThi = new ObservableCollection<KhachThueHienThiItem>(dsLoc);
            TongHienThiText = $"Hiển thị {dsLoc.Count} / {tatCaKhach.Count} khách";
        }

        // Hàm dùng chung kiểm tra dữ liệu đầu vào cho Thêm & Sửa
        // Hàm dùng chung kiểm tra dữ liệu đầu vào cho Thêm & Sửa
        private bool KiemTraDuLieuHopLe(int? idBoQua, out string hoTenChuan, out string cccdChuan, out string sdtChuan)
        {
            hoTenChuan = HoTen.Trim();
            cccdChuan = CCCD.Trim();
            sdtChuan = SoDienThoai.Trim();

            if (string.IsNullOrWhiteSpace(hoTenChuan) || string.IsNullOrWhiteSpace(cccdChuan) || string.IsNullOrWhiteSpace(sdtChuan))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Họ tên, Số CCCD và Số điện thoại!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(cccdChuan, @"^\d{9}$|^\d{12}$"))
            {
                MessageBox.Show("Số CCCD/CMND không hợp lệ (chỉ gồm 9 hoặc 12 chữ số)!", "Sai định dạng CCCD", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(sdtChuan, @"^0\d{9,10}$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ (gồm 10–11 chữ số và bắt đầu bằng số 0)!", "Sai định dạng SĐT", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (NgaySinh.Date > DateTime.Today.AddYears(-15) || NgaySinh.Year < 1920)
            {
                MessageBox.Show("Ngày sinh không hợp lệ (khách thuê cần từ 15 tuổi trở lên)!", "Ngày sinh không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Gán sang biến cục bộ để dùng hợp lệ bên trong lambda của LINQ
            string cccdCheck = cccdChuan;
            string sdtCheck = sdtChuan;

            if (_db.KhachThues.Any(k => k.CCCD == cccdCheck && (!idBoQua.HasValue || k.Id != idBoQua.Value)))
            {
                MessageBox.Show($"Số CCCD '{cccdCheck}' đã tồn tại trong hệ thống!", "Trùng CCCD", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (_db.KhachThues.Any(k => k.SoDienThoai == sdtCheck && (!idBoQua.HasValue || k.Id != idBoQua.Value)))
            {
                MessageBox.Show($"Số điện thoại '{sdtCheck}' đã được đăng ký cho khách thuê khác!", "Trùng số điện thoại", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        [RelayCommand]
        private void Them()
        {
            if (!KiemTraDuLieuHopLe(null, out string hoTenChuan, out string cccdChuan, out string sdtChuan))
                return;

            var khachMoi = new KhachThue
            {
                HoTen = hoTenChuan,
                CCCD = cccdChuan,
                SoDienThoai = sdtChuan,
                NgaySinh = NgaySinh.Date,
                QueQuan = QueQuan.Trim(),
                NgheNghiep = NgheNghiep.Trim()
            };

            _db.KhachThues.Add(khachMoi);
            _db.SaveChanges();
            TaiDanhSach();
            LamMoi();
            MessageBox.Show($"Đã thêm hồ sơ khách thuê '{hoTenChuan}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Sua()
        {
            if (KhachDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một khách thuê trên bảng để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!KiemTraDuLieuHopLe(KhachDangChon.Id, out string hoTenChuan, out string cccdChuan, out string sdtChuan))
                return;

            int idVuaSua = KhachDangChon.Id;

            KhachDangChon.HoTen = hoTenChuan;
            KhachDangChon.CCCD = cccdChuan;
            KhachDangChon.SoDienThoai = sdtChuan;
            KhachDangChon.NgaySinh = NgaySinh.Date;
            KhachDangChon.QueQuan = QueQuan.Trim();
            KhachDangChon.NgheNghiep = NgheNghiep.Trim();

            _db.SaveChanges();
            TaiDanhSach();
            KhachHienThiDangChon = DanhSachKhachHienThi.FirstOrDefault(x => x.Id == idVuaSua);

            MessageBox.Show($"Cập nhật hồ sơ khách '{hoTenChuan}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Xoa()
        {
            if (KhachDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn hồ sơ khách thuê cần xóa trên bảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db.HopDongs.Any(h => h.KhachDaiDienId == KhachDangChon.Id && h.TrangThai == 0))
            {
                MessageBox.Show($"Không thể xóa '{KhachDangChon.HoTen}' vì khách đang đứng tên hợp đồng có hiệu lực!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_db.HopDongs.Any(h => h.KhachDaiDienId == KhachDangChon.Id))
            {
                MessageBox.Show($"Không thể xóa '{KhachDangChon.HoTen}' vì khách đã có lịch sử hợp đồng và hóa đơn trong hệ thống.\n\nHồ sơ cần được giữ lại để đối chiếu báo cáo doanh thu!",
                    "Bảo vệ dữ liệu lịch sử", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn hồ sơ khách '{KhachDangChon.HoTen}'?", "Xác nhận xóa",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.KhachThues.Remove(KhachDangChon);
                _db.SaveChanges();
                TaiDanhSach();
                LamMoi();
                MessageBox.Show("Đã xóa hồ sơ khách thuê thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            KhachHienThiDangChon = null;
            KhachDangChon = null;
            HoTen = string.Empty;
            CCCD = string.Empty;
            SoDienThoai = string.Empty;
            NgaySinh = new DateTime(2002, 1, 1);
            QueQuan = string.Empty;
            NgheNghiep = string.Empty;
        }
    }
}