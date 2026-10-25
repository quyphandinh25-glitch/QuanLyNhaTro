using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

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

        // Thống kê tài chính & Biểu đồ
        [ObservableProperty] private decimal _doanhThuDuKienThang;
        [ObservableProperty] private decimal _tongDaThuThucTe;
        [ObservableProperty] private decimal _tongCongNoChuaThu;
        [ObservableProperty] private decimal _tongChiPhiBaoTri;
        [ObservableProperty] private decimal _loiNhuanRong;

        // Chiều rộng thanh biểu đồ Đồ họa (tính theo pixel trên tổng 400px)
        [ObservableProperty] private double _tyLeLapDayPhanTram;
        [ObservableProperty] private double _doRongThanhLapDay;
        [ObservableProperty] private double _doRongThanhDaThu;
        [ObservableProperty] private double _doRongThanhCongNo;
        [ObservableProperty] private string _trangThaiTaiDuLieu = "✅ Dữ liệu đã đồng bộ từ SQLite";

        public DashboardViewModel(AppDbContext db)
        {
            _db = db;
            _ = TaiThongKeAsync();
        }

        // Áp dụng Đa luồng (Async/Await) để tính toán số liệu thống kê & vẽ biểu đồ
        [RelayCommand]
        public async Task TaiThongKeAsync()
        {
            TrangThaiTaiDuLieu = "⏳ Đang tổng hợp dữ liệu đa luồng...";

            await Task.Run(() =>
            {
                var danhSachPhong = _db.Phongs.AsNoTracking().ToList();
                var danhSachHoaDon = _db.HoaDons.AsNoTracking().ToList();
                var danhSachSuCo = _db.SuCos.AsNoTracking().ToList();

                int tong = danhSachPhong.Count;
                int trong = danhSachPhong.Count(p => p.TrangThai == 0);
                int dangThue = danhSachPhong.Count(p => p.TrangThai == 1);
                int sapHet = danhSachPhong.Count(p => p.TrangThai == 2);
                int baoTri = danhSachPhong.Count(p => p.TrangThai == 3);

                decimal duKien = danhSachPhong
                    .Where(p => p.TrangThai == 1 || p.TrangThai == 2)
                    .Sum(p => p.GiaThueHienTai);

                decimal daThu = danhSachHoaDon.Sum(h => h.SoTienDaDong);
                decimal conNo = danhSachHoaDon.Sum(h => h.ConNo);
                decimal chiPhi = danhSachSuCo.Sum(s => s.ChiPhiSuaChua);
                decimal loiNhuan = daThu - chiPhi;

                double tyLe = tong > 0 ? Math.Round((double)(dangThue + sapHet) / tong * 100, 1) : 0;
                decimal tongPhatSinh = daThu + conNo;

                // Cập nhật lên UI Thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TongSoPhong = tong;
                    SoPhongTrong = trong;
                    SoPhongDangThue = dangThue;
                    SoPhongSapHetHan = sapHet;
                    SoPhongBaoTri = baoTri;

                    DoanhThuDuKienThang = duKien;
                    TongDaThuThucTe = daThu;
                    TongCongNoChuaThu = conNo;
                    TongChiPhiBaoTri = chiPhi;
                    LoiNhuanRong = loiNhuan;

                    TyLeLapDayPhanTram = tyLe;
                    DoRongThanhLapDay = tyLe * 3.6; // Max 360px
                    DoRongThanhDaThu = tongPhatSinh > 0 ? (double)(daThu / tongPhatSinh) * 360 : 0;
                    DoRongThanhCongNo = tongPhatSinh > 0 ? (double)(conNo / tongPhatSinh) * 360 : 0;
                    TrangThaiTaiDuLieu = $"✅ Cập nhật lúc {DateTime.Now:HH:mm:ss}";
                });
            });
        }

        // XUẤT BÁO CÁO DOANH THU & HÓA ĐƠN RA FILE EXCEL (.CSV UTF-8)
        [RelayCommand]
        private async Task XuatBaoCaoExcelAsync()
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Xuất Báo cáo Doanh thu Nhà trọ ra Excel",
                Filter = "Excel CSV File (*.csv)|*.csv",
                FileName = $"BaoCaoDoanhThu_NhaTro8386_{DateTime.Now:ddMMyyyy}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var hoaDons = await _db.HoaDons
                    .Include(h => h.Phong)
                    .Include(h => h.HopDong).ThenInclude(hd => hd!.KhachDaiDien)
                    .OrderByDescending(h => h.Nam).ThenByDescending(h => h.Thang)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine("BÁO CÁO TỔNG HỢP DOANH THU & CÔNG NỢ - NHÀ TRỌ 8386 THỦ ĐỨC");
                sb.AppendLine($"Ngày xuất báo cáo:,{DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine();
                sb.AppendLine("Mã Hóa Đơn,Phòng,Khách Đại Diện,Kỳ Tháng/Năm,Tiền Phòng,Tiền Điện,Tiền Nước,Dịch Vụ,Nợ Cũ,Tổng Cộng,Đã Thu,Còn Nợ");

                foreach (var h in hoaDons)
                {
                    sb.AppendLine($"{h.MaHoaDon},{h.Phong?.MaPhong},{h.HopDong?.KhachDaiDien?.HoTen},{h.Thang}/{h.Nam},{h.TienPhong},{h.TienDien},{h.TienNuoc},{h.TienDichVu},{h.NoCu},{h.TongCong},{h.SoTienDaDong},{h.ConNo}");
                }

                sb.AppendLine();
                sb.AppendLine($"TỔNG CỘNG ĐÃ THU:,{TongDaThuThucTe}");
                sb.AppendLine($"TỔNG CÔNG NỢ CHƯA THU:,{TongCongNoChuaThu}");
                sb.AppendLine($"TỔNG CHI PHÍ SỬA CHỮA:,{TongChiPhiBaoTri}");
                sb.AppendLine($"LỢI NHUẬN RÒNG:,{LoiNhuanRong}");

                // Lưu với UTF-8 BOM để mở bằng Microsoft Excel không bị lỗi font tiếng Việt
                await File.WriteAllTextAsync(saveDialog.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show($"Đã xuất báo cáo Excel thành công tại:\n{saveDialog.FileName}", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // SAO LƯU (BACKUP) CƠ SỞ DỮ LIỆU SQLITE
        [RelayCommand]
        private void SaoLuuDatabase()
        {
            string sourceDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuanLyNhaTro.db");
            if (!File.Exists(sourceDb))
            {
                MessageBox.Show("Không tìm thấy file cơ sở dữ liệu gốc!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Sao lưu Cơ sở dữ liệu Nhà trọ (SQLite)",
                Filter = "SQLite Database (*.db)|*.db",
                FileName = $"Backup_QuanLyNhaTro_{DateTime.Now:ddMMyyyy_HHmm}.db"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.Copy(sourceDb, saveDialog.FileName, true);
                MessageBox.Show($"Đã sao lưu toàn bộ CSDL an toàn tại:\n{saveDialog.FileName}", "Sao lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}