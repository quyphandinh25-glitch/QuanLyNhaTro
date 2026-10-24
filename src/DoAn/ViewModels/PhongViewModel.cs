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
    public partial class PhongViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<Phong> _danhSachPhong = new();
        [ObservableProperty] private ObservableCollection<LoaiPhong> _danhSachLoaiPhong = new();

        [ObservableProperty] private Phong? _phongDangChon;
        [ObservableProperty] private string _tuKhoaTimKiem = string.Empty;

        // Các trường trên Form nhập liệu
        [ObservableProperty] private string _maPhong = string.Empty;
        [ObservableProperty] private string _tenPhong = string.Empty;
        [ObservableProperty] private int _tang = 1;
        [ObservableProperty] private LoaiPhong? _loaiPhongChon;
        [ObservableProperty] private decimal _giaThueHienTai;
        [ObservableProperty] private int _trangThai = 0;
        [ObservableProperty] private string _ghiChu = string.Empty;

        public PhongViewModel(AppDbContext db)
        {
            _db = db;
            TaiDuLieu();
        }

        public void TaiDuLieu()
        {
            DanhSachLoaiPhong = new ObservableCollection<LoaiPhong>(_db.LoaiPhongs.ToList());
            LocDanhSachPhong();
        }

        partial void OnTuKhoaTimKiemChanged(string value) => LocDanhSachPhong();

        // Tự động gợi ý giá thuê khi chọn Loại phòng
        partial void OnLoaiPhongChonChanged(LoaiPhong? value)
        {
            if (value != null && PhongDangChon == null)
            {
                GiaThueHienTai = value.DonGiaCoBan;
            }
        }

        // Đổ dữ liệu lên Form khi click chọn 1 dòng trên bảng
        partial void OnPhongDangChonChanged(Phong? value)
        {
            if (value != null)
            {
                MaPhong = value.MaPhong;
                TenPhong = value.TenPhong;
                Tang = value.Tang;
                LoaiPhongChon = DanhSachLoaiPhong.FirstOrDefault(l => l.Id == value.LoaiPhongId);
                GiaThueHienTai = value.GiaThueHienTai;
                TrangThai = value.TrangThai;
                GhiChu = value.GhiChu ?? string.Empty;
            }
        }

        private void LocDanhSachPhong()
        {
            var query = _db.Phongs.Include(p => p.LoaiPhong).AsQueryable();
            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                query = query.Where(p => p.MaPhong.ToLower().Contains(kw) || p.TenPhong.ToLower().Contains(kw));
            }
            DanhSachPhong = new ObservableCollection<Phong>(query.OrderBy(p => p.Tang).ThenBy(p => p.MaPhong).ToList());
        }

        [RelayCommand]
        private void ThemMoi()
        {
            if (string.IsNullOrWhiteSpace(MaPhong) || string.IsNullOrWhiteSpace(TenPhong) || LoaiPhongChon == null)
            {
                MessageBox.Show("Vui lòng nhập Mã phòng, Tên phòng và chọn Loại phòng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db.Phongs.Any(p => p.MaPhong == MaPhong.Trim()))
            {
                MessageBox.Show("Mã phòng này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var phongMoi = new Phong
            {
                MaPhong = MaPhong.Trim(),
                TenPhong = TenPhong.Trim(),
                Tang = Tang,
                LoaiPhongId = LoaiPhongChon.Id,
                GiaThueHienTai = GiaThueHienTai,
                TrangThai = TrangThai,
                GhiChu = GhiChu.Trim()
            };

            _db.Phongs.Add(phongMoi);
            _db.SaveChanges();
            TaiDuLieu();
            LamMoi();
            MessageBox.Show("Thêm phòng mới thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void CapNhat()
        {
            if (PhongDangChon == null || LoaiPhongChon == null)
            {
                MessageBox.Show("Vui lòng chọn một phòng trên bảng để sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PhongDangChon.TenPhong = TenPhong.Trim();
            PhongDangChon.Tang = Tang;
            PhongDangChon.LoaiPhongId = LoaiPhongChon.Id;
            PhongDangChon.GiaThueHienTai = GiaThueHienTai;
            PhongDangChon.TrangThai = TrangThai;
            PhongDangChon.GhiChu = GhiChu.Trim();

            _db.SaveChanges();
            LocDanhSachPhong();
            MessageBox.Show("Cập nhật thông tin phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Xoa()
        {
            if (PhongDangChon == null) return;

            if (PhongDangChon.TrangThai == 1 || PhongDangChon.TrangThai == 2)
            {
                MessageBox.Show("Không thể xóa phòng đang có khách thuê!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn xóa phòng {PhongDangChon.MaPhong}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.Phongs.Remove(PhongDangChon);
                _db.SaveChanges();
                LocDanhSachPhong();
                LamMoi();
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            PhongDangChon = null;
            MaPhong = string.Empty;
            TenPhong = string.Empty;
            Tang = 1;
            LoaiPhongChon = DanhSachLoaiPhong.FirstOrDefault();
            GiaThueHienTai = LoaiPhongChon?.DonGiaCoBan ?? 0;
            TrangThai = 0;
            GhiChu = string.Empty;
        }
    }
}