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
    public partial class SuCoViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<SuCo> _danhSachSuCo = new();
        [ObservableProperty] private ObservableCollection<Phong> _danhSachPhong = new();
        [ObservableProperty] private SuCo? _suCoDangChon;

        public List<string> DanhSachLocTrangThai { get; } = new()
        {
            "Tất cả sự cố",
            "0 - Mới tiếp nhận",
            "1 - Đang xử lý sửa chữa",
            "2 - Đã hoàn thành"
        };

        [ObservableProperty] private string _locTrangThaiChon = "Tất cả sự cố";

        // Các trường nhập liệu
        [ObservableProperty] private Phong? _phongChon;
        [ObservableProperty] private string _tieuDe = string.Empty;
        [ObservableProperty] private string _moTaChiTiet = string.Empty;
        [ObservableProperty] private decimal _chiPhiSuaChua;
        [ObservableProperty] private int _trangThai = 0;
        [ObservableProperty] private bool _chuyenPhongSangBaoTri = true;

        // Thống kê chi phí bảo trì
        [ObservableProperty] private decimal _tongChiPhiSuaChua;
        [ObservableProperty] private int _soSuCoDangXuLy;

        public SuCoViewModel(AppDbContext db)
        {
            _db = db;
            TaiDuLieu();
        }

        partial void OnLocTrangThaiChonChanged(string value) => TaiDanhSachSuCo();

        partial void OnSuCoDangChonChanged(SuCo? value)
        {
            if (value != null)
            {
                PhongChon = DanhSachPhong.FirstOrDefault(p => p.Id == value.PhongId);
                TieuDe = value.TieuDe;
                MoTaChiTiet = value.MoTaChiTiet;
                ChiPhiSuaChua = value.ChiPhiSuaChua;
                TrangThai = value.TrangThai;
            }
        }

        public void TaiDuLieu()
        {
            DanhSachPhong = new ObservableCollection<Phong>(_db.Phongs.OrderBy(p => p.MaPhong).ToList());
            PhongChon = DanhSachPhong.FirstOrDefault();
            TaiDanhSachSuCo();
        }

        private void TaiDanhSachSuCo()
        {
            var tatCa = _db.SuCos.Include(s => s.Phong).OrderByDescending(s => s.NgayBao).ToList();

            TongChiPhiSuaChua = tatCa.Sum(s => s.ChiPhiSuaChua);
            SoSuCoDangXuLy = tatCa.Count(s => s.TrangThai != 2);

            var ketQua = tatCa.AsEnumerable();
            if (LocTrangThaiChon.StartsWith("0")) ketQua = ketQua.Where(s => s.TrangThai == 0);
            else if (LocTrangThaiChon.StartsWith("1")) ketQua = ketQua.Where(s => s.TrangThai == 1);
            else if (LocTrangThaiChon.StartsWith("2")) ketQua = ketQua.Where(s => s.TrangThai == 2);

            DanhSachSuCo = new ObservableCollection<SuCo>(ketQua);
        }

        [RelayCommand]
        private void ThemSuCo()
        {
            if (PhongChon == null || string.IsNullOrWhiteSpace(TieuDe))
            {
                MessageBox.Show("Vui lòng chọn Phòng và nhập Tiêu đề sự cố!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var suCoMoi = new SuCo
            {
                PhongId = PhongChon.Id,
                TieuDe = TieuDe.Trim(),
                MoTaChiTiet = MoTaChiTiet.Trim(),
                NgayBao = DateTime.Now,
                ChiPhiSuaChua = ChiPhiSuaChua,
                TrangThai = TrangThai
            };

            // Nếu tích chọn chuyển phòng sang trạng thái Bảo trì và sự cố chưa hoàn thành
            if (ChuyenPhongSangBaoTri && TrangThai != 2)
            {
                PhongChon.TrangThai = 3; // 3 = Đang bảo trì
                PhongChon.GhiChu = TieuDe.Trim();
            }

            _db.SuCos.Add(suCoMoi);
            _db.SaveChanges();

            TaiDanhSachSuCo();
            LamMoi();
            MessageBox.Show("Đã ghi nhận sự cố bảo trì thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void CapNhatSuCo()
        {
            if (SuCoDangChon == null || PhongChon == null) return;

            SuCoDangChon.PhongId = PhongChon.Id;
            SuCoDangChon.TieuDe = TieuDe.Trim();
            SuCoDangChon.MoTaChiTiet = MoTaChiTiet.Trim();
            SuCoDangChon.ChiPhiSuaChua = ChiPhiSuaChua;
            SuCoDangChon.TrangThai = TrangThai;

            _db.SaveChanges();
            TaiDanhSachSuCo();
            MessageBox.Show("Đã cập nhật thông tin sự cố!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void HoanTatSuaChua()
        {
            if (SuCoDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một sự cố trên bảng để đánh dấu hoàn thành!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SuCoDangChon.TrangThai = 2; // 2 = Đã hoàn thành

            // Kiểm tra nếu phòng đang ở trạng thái Bảo trì (3) thì tự động mở lại phòng
            var phong = _db.Phongs.FirstOrDefault(p => p.Id == SuCoDangChon.PhongId);
            if (phong != null && phong.TrangThai == 3)
            {
                bool coHopDong = _db.HopDongs.Any(h => h.PhongId == phong.Id && h.TrangThai == 0);
                phong.TrangThai = coHopDong ? 1 : 0;
                phong.GhiChu = "Đã sửa chữa xong";
            }

            _db.SaveChanges();
            TaiDanhSachSuCo();
            MessageBox.Show("Đã hoàn tất sửa chữa và khôi phục trạng thái hoạt động cho phòng!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void XoaSuCo()
        {
            if (SuCoDangChon == null) return;

            if (MessageBox.Show($"Xóa bản ghi sự cố '{SuCoDangChon.TieuDe}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.SuCos.Remove(SuCoDangChon);
                _db.SaveChanges();
                TaiDanhSachSuCo();
                LamMoi();
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            SuCoDangChon = null;
            TieuDe = string.Empty;
            MoTaChiTiet = string.Empty;
            ChiPhiSuaChua = 0;
            TrangThai = 0;
            ChuyenPhongSangBaoTri = true;
        }
    }
}