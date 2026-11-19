using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Data;
using DoAn.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace DoAn.ViewModels
{
    // DTO hiển thị bảng Hợp đồng sắp hết hạn trên Dashboard
    public class HopDongSapHetItem
    {
        public string MaHopDong { get; set; } = string.Empty;
        public string MaPhong { get; set; } = string.Empty;
        public string TenKhach { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = "---";
        public string NgayHetHanText { get; set; } = string.Empty;
        public string CanhBaoText { get; set; } = string.Empty;
        public string MauNenBadge { get; set; } = "#FEF3C7";
        public string MauChuBadge { get; set; } = "#B45309";
    }

    // DTO hiển thị bảng Sự cố & Bảo trì chờ xử lý trên Dashboard
    public class SuCoChoXuLyItem
    {
        public string MaPhong { get; set; } = string.Empty;
        public string TieuDe { get; set; } = string.Empty;
        public string ChiPhiText { get; set; } = "0 đ";
        public string TrangThaiText { get; set; } = "Đang xử lý";
        public string MauNenBadge { get; set; } = "#FEE2E2";
        public string MauChuBadge { get; set; } = "#B91C1C";
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private bool _dangTai;

        // Thống kê quy mô & trạng thái phòng
        [ObservableProperty] private int _tongSoPhong;
        [ObservableProperty] private int _soPhongTrong;
        [ObservableProperty] private int _soPhongDangThue;
        [ObservableProperty] private int _soPhongSapHetHan;
        [ObservableProperty] private int _soPhongBaoTri;

        // Thống kê tài chính
        [ObservableProperty] private decimal _doanhThuDuKienThang;
        [ObservableProperty] private decimal _tongDaThuThucTe;
        [ObservableProperty] private decimal _tongCongNoChuaThu;
        [ObservableProperty] private decimal _tongChiPhiBaoTri;
        [ObservableProperty] private decimal _loiNhuanRong;

        // Phần trăm cho thanh ProgressBar co giãn (0 -> 100%)
        private double _tyLeLapDayPhanTram;
        public double TyLeLapDayPhanTram
        {
            get => _tyLeLapDayPhanTram;
            set => SetProperty(ref _tyLeLapDayPhanTram, value);
        }

        private string _trangThaiTaiDuLieu = "✅ Dữ liệu đã đồng bộ từ SQLite";
        public string TrangThaiTaiDuLieu
        {
            get => _trangThaiTaiDuLieu;
            set => SetProperty(ref _trangThaiTaiDuLieu, value);
        }
        private double _tyLeDaThuPhanTram;
        public double TyLeDaThuPhanTram
        {
            get => _tyLeDaThuPhanTram;
            set => SetProperty(ref _tyLeDaThuPhanTram, value);
        }
        private double _tyLeCongNoPhanTram;
        public double TyLeCongNoPhanTram
        {
            get => _tyLeCongNoPhanTram;
            set => SetProperty(ref _tyLeCongNoPhanTram, value);
        }

        private ObservableCollection<HopDongSapHetItem> _danhSachHopDongSapHet = new();
        public ObservableCollection<HopDongSapHetItem> DanhSachHopDongSapHet
        {
            get => _danhSachHopDongSapHet;
            set => SetProperty(ref _danhSachHopDongSapHet, value);
        }

        private int _soHopDongSapHet30Ngay;
        public int SoHopDongSapHet30Ngay
        {
            get => _soHopDongSapHet30Ngay;
            set => SetProperty(ref _soHopDongSapHet30Ngay, value);
        }

        private Visibility _hienThiTrongHopDong = Visibility.Collapsed;
        public Visibility HienThiTrongHopDong
        {
            get => _hienThiTrongHopDong;
            set => SetProperty(ref _hienThiTrongHopDong, value);
        }

        private ObservableCollection<SuCoChoXuLyItem> _danhSachSuCoChoXuLy = new();
        public ObservableCollection<SuCoChoXuLyItem> DanhSachSuCoChoXuLy
        {
            get => _danhSachSuCoChoXuLy;
            set => SetProperty(ref _danhSachSuCoChoXuLy, value);
        }

        private int _soSuCoChoXuLy;
        public int SoSuCoChoXuLy
        {
            get => _soSuCoChoXuLy;
            set => SetProperty(ref _soSuCoChoXuLy, value);
        }

        private Visibility _hienThiTrongSuCo = Visibility.Collapsed;
        public Visibility HienThiTrongSuCo
        {
            get => _hienThiTrongSuCo;
            set => SetProperty(ref _hienThiTrongSuCo, value);
        }

        public DashboardViewModel(AppDbContext db)
        {
            _db = db;
            _ = TaiThongKeAsync();
        }

        // Tải thống kê bất đồng bộ tuần tự chuẩn EF Core (không xung đột DbContext)
        [RelayCommand]
        public async Task TaiThongKeAsync()
        {
            if (_dangTai) return;
            _dangTai = true;

            try
            {
                TrangThaiTaiDuLieu = "⏳ Đang tổng hợp dữ liệu từ SQLite...";

                // 1. Lấy danh sách phòng kèm hợp đồng và sự cố để đồng bộ trạng thái chuẩn
                var danhSachPhong = await _db.Phongs
                    .Include(p => p.HopDongs).ThenInclude(h => h.KhachDaiDien)
                    .Include(p => p.SuCos)
                    .OrderBy(p => p.MaPhong)
                    .ToListAsync();

                bool coThayDoiTrangThai = false;
                foreach (var p in danhSachPhong)
                {
                    if (p.TrangThai != 3)
                    {
                        var hdHieuLuc = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                        int trangThaiMoi = 0;
                        if (hdHieuLuc != null)
                        {
                            double soNgayConLai = (hdHieuLuc.NgayKetThuc.Date - DateTime.Today).TotalDays;
                            trangThaiMoi = soNgayConLai <= 15 ? 2 : 1;
                        }

                        if (p.TrangThai != trangThaiMoi)
                        {
                            p.TrangThai = trangThaiMoi;
                            coThayDoiTrangThai = true;
                        }
                    }
                }

                if (coThayDoiTrangThai)
                {
                    await _db.SaveChangesAsync();
                }

                // 2. Lấy hóa đơn để tính doanh thu & công nợ thực tế
                var danhSachHoaDon = await _db.HoaDons.AsNoTracking().ToListAsync();

                int tong = danhSachPhong.Count;
                int trong = danhSachPhong.Count(p => p.TrangThai == 0);
                int dangThue = danhSachPhong.Count(p => p.TrangThai == 1);
                int sapHet = danhSachPhong.Count(p => p.TrangThai == 2);
                int baoTri = danhSachPhong.Count(p => p.TrangThai == 3);

                // Tiền phòng dự kiến/tháng tính theo giá chốt các hợp đồng đang hiệu lực
                var hopDongDangHieuLuc = danhSachPhong
                    .SelectMany(p => p.HopDongs.Where(h => h.TrangThai == 0).Select(h => new { Phong = p, HopDong = h }))
                    .ToList();

                decimal duKien = hopDongDangHieuLuc.Sum(x => x.HopDong.GiaThueChot);
                decimal daThu = danhSachHoaDon.Sum(h => h.SoTienDaDong);

                // Công thức tính tổng công nợ gốc theo từng hợp đồng (tránh cộng trùng NoCu giữa các kỳ)
                decimal conNo = danhSachHoaDon
                    .GroupBy(h => h.HopDongId)
                    .Sum(g => Math.Max(0, g.Sum(h => (h.TongCong - h.NoCu) - h.SoTienDaDong)));

                var tatCaSuCo = danhSachPhong
                    .SelectMany(p => p.SuCos.Select(s => new { Phong = p, SuCo = s }))
                    .ToList();

                decimal chiPhi = tatCaSuCo.Sum(x => x.SuCo.ChiPhiSuaChua);
                decimal loiNhuan = daThu - chiPhi;

                double tyLeLapDay = tong > 0 ? Math.Round((double)(dangThue + sapHet) / tong * 100, 1) : 0;
                decimal tongPhatSinh = daThu + conNo;
                double tyLeThu = tongPhatSinh > 0 ? Math.Round((double)(daThu / tongPhatSinh) * 100, 1) : 0;
                double tyLeNo = tongPhatSinh > 0 ? Math.Round((double)(conNo / tongPhatSinh) * 100, 1) : 0;

                // 3. Lọc danh sách Hợp đồng sắp hết hạn trong vòng 30 ngày tới (bao gồm cả quá hạn)
                var dsSapHet = hopDongDangHieuLuc
                    .Select(x => new
                    {
                        x.Phong.MaPhong,
                        x.HopDong,
                        SoNgayConLai = (int)Math.Ceiling((x.HopDong.NgayKetThuc.Date - DateTime.Today).TotalDays)
                    })
                    .Where(x => x.SoNgayConLai <= 30)
                    .OrderBy(x => x.SoNgayConLai)
                    .Select(x =>
                    {
                        bool khanCap = x.SoNgayConLai <= 7;
                        string canhBao = x.SoNgayConLai < 0
                            ? $"Quá hạn {Math.Abs(x.SoNgayConLai)} ngày"
                            : (x.SoNgayConLai == 0 ? "Hết hạn hôm nay" : $"Còn {x.SoNgayConLai} ngày");

                        return new HopDongSapHetItem
                        {
                            MaHopDong = x.HopDong.MaHopDong,
                            MaPhong = x.MaPhong,
                            TenKhach = x.HopDong.KhachDaiDien?.HoTen ?? "---",
                            SoDienThoai = x.HopDong.KhachDaiDien?.SoDienThoai ?? "---",
                            NgayHetHanText = x.HopDong.NgayKetThuc.ToString("dd/MM/yyyy"),
                            CanhBaoText = canhBao,
                            MauNenBadge = khanCap ? "#FEE2E2" : "#FEF3C7",
                            MauChuBadge = khanCap ? "#B91C1C" : "#B45309"
                        };
                    })
                    .ToList();

                // 4. Lọc danh sách Sự cố & Bảo trì chưa hoàn tất (TrangThai != 2)
                var dsSuCo = tatCaSuCo
                    .Where(x => x.SuCo.TrangThai != 2)
                    .OrderBy(x => x.Phong.MaPhong)
                    .Select(x => new SuCoChoXuLyItem
                    {
                        MaPhong = x.Phong.MaPhong,
                        TieuDe = x.SuCo.TieuDe,
                        ChiPhiText = $"{x.SuCo.ChiPhiSuaChua:N0} đ",
                        TrangThaiText = x.SuCo.TrangThai == 0 ? "Chờ xử lý" : "Đang sửa chữa",
                        MauNenBadge = x.SuCo.TrangThai == 0 ? "#FEF3C7" : "#FEE2E2",
                        MauChuBadge = x.SuCo.TrangThai == 0 ? "#B45309" : "#B91C1C"
                    })
                    .ToList();

                // Cập nhật lên giao diện
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

                TyLeLapDayPhanTram = tyLeLapDay;
                TyLeDaThuPhanTram = tyLeThu;
                TyLeCongNoPhanTram = tyLeNo;

                DanhSachHopDongSapHet = new ObservableCollection<HopDongSapHetItem>(dsSapHet);
                SoHopDongSapHet30Ngay = dsSapHet.Count;
                HienThiTrongHopDong = dsSapHet.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                DanhSachSuCoChoXuLy = new ObservableCollection<SuCoChoXuLyItem>(dsSuCo);
                SoSuCoChoXuLy = dsSuCo.Count;
                HienThiTrongSuCo = dsSuCo.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                TrangThaiTaiDuLieu = $"✅ Đồng bộ lúc {DateTime.Now:HH:mm:ss} • {DateTime.Now:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                TrangThaiTaiDuLieu = $"⚠️ Lỗi tải dữ liệu: {ex.Message}";
            }
            finally
            {
                _dangTai = false;
            }
        }

        // XUẤT BÁO CÁO DOANH THU & HÓA ĐƠN RA FILE EXCEL (.CSV UTF-8)
        [RelayCommand]
        private async Task XuatBaoCaoExcelAsync()
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Xuất Báo cáo Doanh thu Nhà trọ ra Excel",
                Filter = "Excel CSV File (*.csv)|*.csv",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:ddMMyyyy}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var hoaDons = await _db.HoaDons
                        .AsNoTracking()
                        .Include(h => h.Phong)
                        .Include(h => h.HopDong).ThenInclude(hd => hd!.KhachDaiDien)
                        .OrderByDescending(h => h.Nam).ThenByDescending(h => h.Thang)
                        .ToListAsync();

                    string tenNhaTro = AuthService.CurrentUser?.TenNhaTro ?? "NHÀ TRỌ 8386";
                    var sb = new StringBuilder();
                    sb.AppendLine($"BÁO CÁO TỔNG HỢP DOANH THU & CÔNG NỢ - {tenNhaTro.ToUpper()}");
                    sb.AppendLine($"Ngày xuất báo cáo:,{DateTime.Now:dd/MM/yyyy HH:mm}");
                    sb.AppendLine();
                    sb.AppendLine("Mã Hóa Đơn,Phòng,Khách Đại Diện,Kỳ Tháng/Năm,Tiền Phòng,Tiền Điện,Tiền Nước,Dịch Vụ,Nợ Cũ,Tổng Cộng,Đã Thu,Còn Nợ");

                    foreach (var h in hoaDons)
                    {
                        string tenKhach = (h.HopDong?.KhachDaiDien?.HoTen ?? "").Replace(",", " ");
                        sb.AppendLine($"{h.MaHoaDon},{h.Phong?.MaPhong},{tenKhach},{h.Thang}/{h.Nam},{h.TienPhong},{h.TienDien},{h.TienNuoc},{h.TienDichVu},{h.NoCu},{h.TongCong},{h.SoTienDaDong},{h.ConNo}");
                    }

                    sb.AppendLine();
                    sb.AppendLine($"TỔNG CỘNG ĐÃ THU:,{TongDaThuThucTe}");
                    sb.AppendLine($"TỔNG CÔNG NỢ GỐC CHƯA THU:,{TongCongNoChuaThu}");
                    sb.AppendLine($"TỔNG CHI PHÍ SỬA CHỮA:,{TongChiPhiBaoTri}");
                    sb.AppendLine($"LỢI NHUẬN RÒNG:,{LoiNhuanRong}");

                    await File.WriteAllTextAsync(saveDialog.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show($"Đã xuất báo cáo Excel thành công tại:\n{saveDialog.FileName}", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xuất file báo cáo:\n{ex.Message}", "Lỗi xuất file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                try
                {
                    File.Copy(sourceDb, saveDialog.FileName, true);
                    MessageBox.Show($"Đã sao lưu toàn bộ CSDL an toàn tại:\n{saveDialog.FileName}", "Sao lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Sao lưu thất bại:\n{ex.Message}", "Lỗi sao lưu", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}