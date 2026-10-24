using System;
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
    public partial class HopDongViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<HopDong> _danhSachHopDong = new();
        [ObservableProperty] private ObservableCollection<Phong> _danhSachPhongTrong = new();
        [ObservableProperty] private ObservableCollection<KhachThue> _danhSachKhachThue = new();

        [ObservableProperty] private HopDong? _hopDongDangChon;

        [ObservableProperty] private string _maHopDong = string.Empty;
        [ObservableProperty] private Phong? _phongChon;
        [ObservableProperty] private KhachThue? _khachChon;
        [ObservableProperty] private DateTime _ngayBatDau = DateTime.Now;
        [ObservableProperty] private DateTime _ngayKetThuc = DateTime.Now.AddMonths(12);
        [ObservableProperty] private decimal _giaThueChot;
        [ObservableProperty] private decimal _tienCoc;
        [ObservableProperty] private int _soNguoiO = 1;
        [ObservableProperty] private int _soLuongXe = 1;

        public HopDongViewModel(AppDbContext db)
        {
            _db = db;
            TaiDuLieu();
            TaoMaHopDongMoi();
        }

        public void TaiDuLieu()
        {
            DanhSachHopDong = new ObservableCollection<HopDong>(
                _db.HopDongs
                   .Include(h => h.Phong)
                   .Include(h => h.KhachDaiDien)
                   .OrderByDescending(h => h.Id)
                   .ToList());

            // Lấy các phòng đang Trống (TrangThai == 0) để cho thuê mới
            DanhSachPhongTrong = new ObservableCollection<Phong>(
                _db.Phongs.Where(p => p.TrangThai == 0).OrderBy(p => p.MaPhong).ToList());

            DanhSachKhachThue = new ObservableCollection<KhachThue>(_db.KhachThues.ToList());
        }

        // Khi chọn phòng trống -> Tự động lấy đơn giá phòng làm Giá thuê chốt & Tiền cọc
        partial void OnPhongChonChanged(Phong? value)
        {
            if (value != null)
            {
                GiaThueChot = value.GiaThueHienTai;
                TienCoc = value.GiaThueHienTai;
            }
        }

        private void TaoMaHopDongMoi()
        {
            int count = _db.HopDongs.Count() + 1;
            MaHopDong = $"HD-{DateTime.Now:yyyy}-{count:D3}";
        }

        [RelayCommand]
        private void LapHopDong()
        {
            if (PhongChon == null || KhachChon == null)
            {
                MessageBox.Show("Vui lòng chọn Phòng trống và Khách thuê đại diện!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NgayKetThuc <= NgayBatDau)
            {
                MessageBox.Show("Ngày kết thúc hợp đồng phải lớn hơn Ngày bắt đầu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var hopDongMoi = new HopDong
            {
                MaHopDong = MaHopDong.Trim(),
                PhongId = PhongChon.Id,
                KhachDaiDienId = KhachChon.Id,
                NgayLap = DateTime.Now,
                NgayBatDau = NgayBatDau,
                NgayKetThuc = NgayKetThuc,
                GiaThueChot = GiaThueChot,
                TienCoc = TienCoc,
                SoNguoiO = SoNguoiO,
                SoLuongXe = SoLuongXe,
                TrangThai = 0 // 0 = Đang hiệu lực
            };

            // Tự động đổi trạng thái phòng sang 1 (Đang thuê)
            PhongChon.TrangThai = 1;

            _db.HopDongs.Add(hopDongMoi);
            _db.SaveChanges();

            TaiDuLieu();
            LamMoi();
            MessageBox.Show("Lập hợp đồng thành công! Phòng đã chuyển sang trạng thái Đang thuê.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void GiaHan6Thang()
        {
            if (HopDongDangChon == null || HopDongDangChon.TrangThai != 0)
            {
                MessageBox.Show("Vui lòng chọn một hợp đồng đang hiệu lực để gia hạn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            HopDongDangChon.NgayKetThuc = HopDongDangChon.NgayKetThuc.AddMonths(6);
            if (HopDongDangChon.Phong != null)
            {
                HopDongDangChon.Phong.TrangThai = 1; // Đưa từ Sắp hết hạn (2) về Đang thuê (1)
            }

            _db.SaveChanges();
            TaiDuLieu();
            MessageBox.Show($"Đã gia hạn hợp đồng {HopDongDangChon.MaHopDong} thêm 6 tháng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void TraPhongThanhLy()
        {
            if (HopDongDangChon == null || HopDongDangChon.TrangThai != 0)
            {
                MessageBox.Show("Vui lòng chọn một hợp đồng đang hiệu lực trên bảng để trả phòng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tính tổng nợ hóa đơn chưa đóng của hợp đồng này
            decimal tongNoHoaDon = _db.HoaDons
                .Where(b => b.HopDongId == HopDongDangChon.Id && b.TrangThai != 2)
                .Sum(b => b.ConNo);

            decimal tienHoanCoc = HopDongDangChon.TienCoc - tongNoHoaDon;

            string thongBao = $"Xác nhận thanh lý hợp đồng {HopDongDangChon.MaHopDong} (Phòng {HopDongDangChon.Phong?.MaPhong}):\n\n" +
                              $"• Tiền cọc ban đầu: {HopDongDangChon.TienCoc:N0} VNĐ\n" +
                              $"• Nợ hóa đơn chưa đóng: {tongNoHoaDon:N0} VNĐ\n" +
                              $"👉 SỐ TIỀN HOÀN CỌC CHO KHÁCH: {tienHoanCoc:N0} VNĐ\n\n" +
                              $"Bạn có chắc chắn muốn hoàn tất trả phòng?";

            if (MessageBox.Show(thongBao, "Thanh lý Hợp đồng & Hoàn cọc", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                HopDongDangChon.TrangThai = 1; // 1 = Đã thanh lý/Kết thúc
                HopDongDangChon.TienHoanCoc = tienHoanCoc;
                HopDongDangChon.NgayTraPhongThucTe = DateTime.Now;

                // Đổi trạng thái phòng về 0 (Trống)
                if (HopDongDangChon.Phong != null)
                {
                    HopDongDangChon.Phong.TrangThai = 0;
                }

                _db.SaveChanges();
                TaiDuLieu();
                MessageBox.Show("Đã thanh lý hợp đồng và chuyển phòng về trạng thái Trống!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            HopDongDangChon = null;
            TaoMaHopDongMoi();
            PhongChon = DanhSachPhongTrong.FirstOrDefault();
            KhachChon = DanhSachKhachThue.FirstOrDefault();
            NgayBatDau = DateTime.Now;
            NgayKetThuc = DateTime.Now.AddMonths(12);
            SoNguoiO = 1;
            SoLuongXe = 1;
        }
    }
}