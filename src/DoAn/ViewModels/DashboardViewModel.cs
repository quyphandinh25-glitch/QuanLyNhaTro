using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DoAn.Data;

namespace DoAn.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private int _tongSoPhong;
        [ObservableProperty] private int _soPhongTrong;
        [ObservableProperty] private int _soPhongDangThue;
        [ObservableProperty] private int _soPhongSapHetHan;
        [ObservableProperty] private int _soPhongBaoTri;
        [ObservableProperty] private decimal _doanhThuDuKienThang;

        public DashboardViewModel(AppDbContext db)
        {
            _db = db;
            TaiThongKe();
        }

        public void TaiThongKe()
        {
            var danhSachPhong = _db.Phongs.ToList();
            TongSoPhong = danhSachPhong.Count;
            SoPhongTrong = danhSachPhong.Count(p => p.TrangThai == 0);
            SoPhongDangThue = danhSachPhong.Count(p => p.TrangThai == 1);
            SoPhongSapHetHan = danhSachPhong.Count(p => p.TrangThai == 2);
            SoPhongBaoTri = danhSachPhong.Count(p => p.TrangThai == 3);

            // Tính tổng tiền phòng của các phòng đang có người ở (Trạng thái 1 và 2)
            DoanhThuDuKienThang = danhSachPhong
                .Where(p => p.TrangThai == 1 || p.TrangThai == 2)
                .Sum(p => p.GiaThueHienTai);
        }
    }
}