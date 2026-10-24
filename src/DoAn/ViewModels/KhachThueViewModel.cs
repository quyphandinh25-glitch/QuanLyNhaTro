using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using DoAn.Models;

namespace DoAn.ViewModels
{
    public partial class KhachThueViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<KhachThue> _danhSachKhach = new();
        [ObservableProperty] private KhachThue? _khachDangChon;
        [ObservableProperty] private string _tuKhoaTimKiem = string.Empty;

        [ObservableProperty] private string _hoTen = string.Empty;
        [ObservableProperty] private string _cCCD = string.Empty;
        [ObservableProperty] private string _soDienThoai = string.Empty;
        [ObservableProperty] private DateTime _ngaySinh = new DateTime(2002, 1, 1);
        [ObservableProperty] private string _queQuan = string.Empty;
        [ObservableProperty] private string _ngheNghiep = string.Empty;

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
                QueQuan = value.QueQuan;
                NgheNghiep = value.NgheNghiep ?? string.Empty;
            }
        }

        public void TaiDanhSach()
        {
            var query = _db.KhachThues.AsQueryable();
            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                query = query.Where(k => k.HoTen.ToLower().Contains(kw) || k.CCCD.Contains(kw) || k.SoDienThoai.Contains(kw));
            }
            DanhSachKhach = new ObservableCollection<KhachThue>(query.ToList());
        }

        [RelayCommand]
        private void Them()
        {
            if (string.IsNullOrWhiteSpace(HoTen) || string.IsNullOrWhiteSpace(CCCD) || string.IsNullOrWhiteSpace(SoDienThoai))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Họ tên, CCCD và Số điện thoại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db.KhachThues.Any(k => k.CCCD == CCCD.Trim()))
            {
                MessageBox.Show("Số CCCD này đã tồn tại trong hệ thống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var khachMoi = new KhachThue
            {
                HoTen = HoTen.Trim(),
                CCCD = CCCD.Trim(),
                SoDienThoai = SoDienThoai.Trim(),
                NgaySinh = NgaySinh,
                QueQuan = QueQuan.Trim(),
                NgheNghiep = NgheNghiep.Trim()
            };

            _db.KhachThues.Add(khachMoi);
            _db.SaveChanges();
            TaiDanhSach();
            LamMoi();
            MessageBox.Show("Thêm hồ sơ khách thuê thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Sua()
        {
            if (KhachDangChon == null) return;

            KhachDangChon.HoTen = HoTen.Trim();
            KhachDangChon.CCCD = CCCD.Trim();
            KhachDangChon.SoDienThoai = SoDienThoai.Trim();
            KhachDangChon.NgaySinh = NgaySinh;
            KhachDangChon.QueQuan = QueQuan.Trim();
            KhachDangChon.NgheNghiep = NgheNghiep.Trim();

            _db.SaveChanges();
            TaiDanhSach();
            MessageBox.Show("Cập nhật hồ sơ khách thuê thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Xoa()
        {
            if (KhachDangChon == null) return;

            if (_db.HopDongs.Any(h => h.KhachDaiDienId == KhachDangChon.Id && h.TrangThai == 0))
            {
                MessageBox.Show("Khách thuê này đang đứng tên hợp đồng có hiệu lực, không thể xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Xóa hồ sơ khách {KhachDangChon.HoTen}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.KhachThues.Remove(KhachDangChon);
                _db.SaveChanges();
                TaiDanhSach();
                LamMoi();
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
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