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
    public class TrangThaiPhongOption
    {
        public int Ma { get; set; }
        public string Ten { get; set; } = string.Empty;
    }

    // Class hiển thị từng dòng Phòng trên DataGrid kèm Badge màu và Khách đang ở
    public class PhongHienThiItem
    {
        public Phong PhongGoc { get; set; } = null!;
        public int Id => PhongGoc.Id;
        public string MaPhong => PhongGoc.MaPhong;
        public string TenPhong => PhongGoc.TenPhong;
        public int Tang => PhongGoc.Tang;
        public int LoaiPhongId => PhongGoc.LoaiPhongId;
        public string TenLoaiPhong => PhongGoc.LoaiPhong?.TenLoai ?? "Tiêu chuẩn";
        public decimal GiaThueHienTai => PhongGoc.GiaThueHienTai;
        public int TrangThai => PhongGoc.TrangThai;
        public string GhiChu => PhongGoc.GhiChu ?? string.Empty;
        public string TenKhachHienTai { get; set; } = "(Trống)";

        public string TenTrangThai => TrangThai switch
        {
            0 => "Phòng trống",
            1 => "Đang thuê",
            2 => "Sắp hết hạn",
            3 => "Đang bảo trì",
            _ => "Không rõ"
        };

        public string MauChamTron => TrangThai switch
        {
            0 => "#10B981",
            1 => "#2563EB",
            2 => "#F59E0B",
            3 => "#EF4444",
            _ => "#64748B"
        };

        public string MauChuBadge => TrangThai switch
        {
            0 => "#047857",
            1 => "#1D4ED8",
            2 => "#B45309",
            3 => "#B91C1C",
            _ => "#334155"
        };

        public string MauNenBadge => TrangThai switch
        {
            0 => "#D1FAE5",
            1 => "#DBEAFE",
            2 => "#FEF3C7",
            3 => "#FEE2E2",
            _ => "#F1F5F9"
        };
    }

    public partial class PhongViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        // Danh sách lựa chọn cho ComboBox Trạng thái trên Form & Bộ lọc
        public List<TrangThaiPhongOption> DanhSachTrangThaiForm { get; } = new()
        {
            new TrangThaiPhongOption { Ma = 0, Ten = "Phòng trống" },
            new TrangThaiPhongOption { Ma = 1, Ten = "Đang thuê" },
            new TrangThaiPhongOption { Ma = 2, Ten = "Sắp hết hạn" },
            new TrangThaiPhongOption { Ma = 3, Ten = "Đang bảo trì" }
        };

        public List<string> DanhSachTrangThaiLoc { get; } = new()
        {
            "Tất cả trạng thái",
            "Phòng trống",
            "Đang thuê",
            "Sắp hết hạn",
            "Đang bảo trì"
        };

        private ObservableCollection<PhongHienThiItem> _danhSachPhong = new();
        public ObservableCollection<PhongHienThiItem> DanhSachPhong
        {
            get => _danhSachPhong;
            set => SetProperty(ref _danhSachPhong, value);
        }

        private ObservableCollection<LoaiPhong> _danhSachLoaiPhong = new();
        public ObservableCollection<LoaiPhong> DanhSachLoaiPhong
        {
            get => _danhSachLoaiPhong;
            set => SetProperty(ref _danhSachLoaiPhong, value);
        }

        private PhongHienThiItem? _phongDangChon;
        public PhongHienThiItem? PhongDangChon
        {
            get => _phongDangChon;
            set
            {
                if (SetProperty(ref _phongDangChon, value) && value != null)
                {
                    MaPhong = value.MaPhong;
                    TenPhong = value.TenPhong;
                    Tang = value.Tang;
                    LoaiPhongChon = DanhSachLoaiPhong.FirstOrDefault(l => l.Id == value.LoaiPhongId);
                    GiaThueHienTai = value.GiaThueHienTai;
                    TrangThai = value.TrangThai;
                    GhiChu = value.GhiChu;
                }
            }
        }

        private string _tuKhoaTimKiem = string.Empty;
        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set
            {
                if (SetProperty(ref _tuKhoaTimKiem, value))
                    LocDanhSachPhong();
            }
        }

        private string _trangThaiLocChon = "Tất cả trạng thái";
        public string TrangThaiLocChon
        {
            get => _trangThaiLocChon;
            set
            {
                if (SetProperty(ref _trangThaiLocChon, value))
                    LocDanhSachPhong();
            }
        }

        private string _tongHienThiText = "0 phòng";
        public string TongHienThiText
        {
            get => _tongHienThiText;
            set => SetProperty(ref _tongHienThiText, value);
        }

        // Các trường trên Form nhập liệu
        private string _maPhong = string.Empty;
        public string MaPhong
        {
            get => _maPhong;
            set => SetProperty(ref _maPhong, value);
        }

        private string _tenPhong = string.Empty;
        public string TenPhong
        {
            get => _tenPhong;
            set => SetProperty(ref _tenPhong, value);
        }

        private int _tang = 1;
        public int Tang
        {
            get => _tang;
            set => SetProperty(ref _tang, value);
        }

        private LoaiPhong? _loaiPhongChon;
        public LoaiPhong? LoaiPhongChon
        {
            get => _loaiPhongChon;
            set
            {
                if (SetProperty(ref _loaiPhongChon, value) && value != null && PhongDangChon == null)
                {
                    GiaThueHienTai = value.DonGiaCoBan;
                }
            }
        }

        private decimal _giaThueHienTai;
        public decimal GiaThueHienTai
        {
            get => _giaThueHienTai;
            set => SetProperty(ref _giaThueHienTai, value);
        }

        private int _trangThai = 0;
        public int TrangThai
        {
            get => _trangThai;
            set => SetProperty(ref _trangThai, value);
        }

        private string _ghiChu = string.Empty;
        public string GhiChu
        {
            get => _ghiChu;
            set => SetProperty(ref _ghiChu, value);
        }

        public PhongViewModel(AppDbContext db)
        {
            _db = db;
            TaiDuLieu();
        }

        public void TaiDuLieu()
        {
            DanhSachLoaiPhong = new ObservableCollection<LoaiPhong>(_db.LoaiPhongs.ToList());
            if (LoaiPhongChon == null)
            {
                LoaiPhongChon = DanhSachLoaiPhong.FirstOrDefault();
                GiaThueHienTai = LoaiPhongChon?.DonGiaCoBan ?? 0;
            }
            LocDanhSachPhong();
        }

        private void LocDanhSachPhong()
        {
            var tatCaPhong = _db.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.HopDongs).ThenInclude(h => h.KhachDaiDien)
                .OrderBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .ToList();

            // Tự động đồng bộ trạng thái phòng khớp với hợp đồng hiệu lực
            bool coThayDoi = false;
            foreach (var p in tatCaPhong)
            {
                if (p.TrangThai != 3)
                {
                    var hd = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                    int ttMoi = 0;
                    if (hd != null)
                    {
                        double conLai = (hd.NgayKetThuc.Date - DateTime.Today).TotalDays;
                        ttMoi = conLai <= 15 ? 2 : 1;
                    }
                    if (p.TrangThai != ttMoi)
                    {
                        p.TrangThai = ttMoi;
                        coThayDoi = true;
                    }
                }
            }
            if (coThayDoi) _db.SaveChanges();

            var ketQua = tatCaPhong.AsEnumerable();

            if (TrangThaiLocChon == "Phòng trống") ketQua = ketQua.Where(p => p.TrangThai == 0);
            else if (TrangThaiLocChon == "Đang thuê") ketQua = ketQua.Where(p => p.TrangThai == 1);
            else if (TrangThaiLocChon == "Sắp hết hạn") ketQua = ketQua.Where(p => p.TrangThai == 2);
            else if (TrangThaiLocChon == "Đang bảo trì") ketQua = ketQua.Where(p => p.TrangThai == 3);

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQua = ketQua.Where(p =>
                    p.MaPhong.ToLower().Contains(kw) ||
                    p.TenPhong.ToLower().Contains(kw) ||
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.ToLower().Contains(kw)) ||
                    p.HopDongs.Any(h => h.TrangThai == 0 && h.KhachDaiDien != null && h.KhachDaiDien.HoTen.ToLower().Contains(kw)));
            }

            var dsHienThi = ketQua.Select(p =>
            {
                var hd = p.HopDongs.FirstOrDefault(h => h.TrangThai == 0);
                return new PhongHienThiItem
                {
                    PhongGoc = p,
                    TenKhachHienTai = hd?.KhachDaiDien?.HoTen ?? (p.TrangThai == 3 ? "Đang sửa chữa" : "---")
                };
            }).ToList();

            DanhSachPhong = new ObservableCollection<PhongHienThiItem>(dsHienThi);
            TongHienThiText = $"Hiển thị {dsHienThi.Count} / {tatCaPhong.Count} phòng";
        }

        [RelayCommand]
        private void ThemMoi()
        {
            string maChuan = MaPhong.Trim().ToUpper();
            string tenChuan = TenPhong.Trim();

            if (string.IsNullOrWhiteSpace(maChuan) || string.IsNullOrWhiteSpace(tenChuan) || LoaiPhongChon == null)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã phòng, Tên phòng và chọn Loại phòng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Tang < 1 || GiaThueHienTai <= 0)
            {
                MessageBox.Show("Số tầng phải >= 1 và Giá thuê phải lớn hơn 0!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db.Phongs.Any(p => p.MaPhong.ToLower() == maChuan.ToLower()))
            {
                MessageBox.Show($"Mã phòng '{maChuan}' đã tồn tại trong hệ thống!", "Trùng mã phòng", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (TrangThai == 1 || TrangThai == 2)
            {
                MessageBox.Show("Phòng mới tạo chưa có hợp đồng nên chỉ có thể chọn trạng thái 'Phòng trống' hoặc 'Đang bảo trì'!", "Ràng buộc nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                TrangThai = 0;
                return;
            }

            var phongMoi = new Phong
            {
                MaPhong = maChuan,
                TenPhong = tenChuan,
                Tang = Tang,
                LoaiPhongId = LoaiPhongChon.Id,
                GiaThueHienTai = GiaThueHienTai,
                TrangThai = TrangThai,
                GhiChu = GhiChu.Trim()
            };

            _db.Phongs.Add(phongMoi);
            _db.SaveChanges();
            LocDanhSachPhong();
            LamMoi();
            MessageBox.Show($"Đã thêm phòng {maChuan} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void CapNhat()
        {
            if (PhongDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một phòng trên bảng để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string maChuan = MaPhong.Trim().ToUpper();
            string tenChuan = TenPhong.Trim();

            if (string.IsNullOrWhiteSpace(maChuan) || string.IsNullOrWhiteSpace(tenChuan) || LoaiPhongChon == null)
            {
                MessageBox.Show("Mã phòng, Tên phòng và Loại phòng không được để trống!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Tang < 1 || GiaThueHienTai <= 0)
            {
                MessageBox.Show("Số tầng phải >= 1 và Giá thuê phải lớn hơn 0!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var phongGoc = PhongDangChon.PhongGoc;

            if (_db.Phongs.Any(p => p.Id != phongGoc.Id && p.MaPhong.ToLower() == maChuan.ToLower()))
            {
                MessageBox.Show($"Mã phòng '{maChuan}' đã được sử dụng cho phòng khác!", "Trùng mã phòng", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool coHopDongHieuLuc = _db.HopDongs.Any(h => h.PhongId == phongGoc.Id && h.TrangThai == 0);

            if (!coHopDongHieuLuc && (TrangThai == 1 || TrangThai == 2))
            {
                MessageBox.Show("Phòng này chưa có hợp đồng thuê hiệu lực!\nVui lòng lập Hợp đồng mới (hoặc Cho thuê nhanh trên Sơ đồ phòng) thay vì tự đổi trạng thái.",
                    "Ràng buộc nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (coHopDongHieuLuc && TrangThai == 0)
            {
                MessageBox.Show("Phòng đang có hợp đồng thuê hiệu lực nên không thể chuyển tay về 'Phòng trống'!\nVui lòng làm thủ tục Trả phòng & Hoàn cọc ở mục Hợp đồng hoặc Sơ đồ phòng.",
                    "Ràng buộc nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            phongGoc.MaPhong = maChuan;
            phongGoc.TenPhong = tenChuan;
            phongGoc.Tang = Tang;
            phongGoc.LoaiPhongId = LoaiPhongChon.Id;
            phongGoc.GiaThueHienTai = GiaThueHienTai;
            phongGoc.TrangThai = TrangThai;
            phongGoc.GhiChu = GhiChu.Trim();

            _db.SaveChanges();

            int idVuaSua = phongGoc.Id;
            LocDanhSachPhong();
            PhongDangChon = DanhSachPhong.FirstOrDefault(p => p.Id == idVuaSua);

            MessageBox.Show($"Cập nhật thông tin phòng {maChuan} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Xoa()
        {
            if (PhongDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn phòng cần xóa trên bảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var phongGoc = PhongDangChon.PhongGoc;

            bool coHopDongHieuLuc = _db.HopDongs.Any(h => h.PhongId == phongGoc.Id && h.TrangThai == 0);
            if (coHopDongHieuLuc || phongGoc.TrangThai == 1 || phongGoc.TrangThai == 2)
            {
                MessageBox.Show($"Không thể xóa phòng {phongGoc.MaPhong} vì đang có khách thuê!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool daPhatSinhDuLieu = _db.HopDongs.Any(h => h.PhongId == phongGoc.Id)
                                 || _db.HoaDons.Any(b => b.PhongId == phongGoc.Id)
                                 || _db.ChiSoDienNuocs.Any(c => c.PhongId == phongGoc.Id)
                                 || _db.SuCos.Any(s => s.PhongId == phongGoc.Id);

            if (daPhatSinhDuLieu)
            {
                MessageBox.Show($"Không thể xóa phòng {phongGoc.MaPhong} vì phòng này đã phát sinh lịch sử Hợp đồng, Hóa đơn, Điện nước hoặc Sự cố.\n\nNếu tạm ngừng cho thuê, bạn hãy chuyển sang trạng thái 'Đang bảo trì' để giữ nguyên dữ liệu báo cáo!",
                    "Bảo vệ dữ liệu lịch sử", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn phòng {phongGoc.MaPhong}?", "Xác nhận xóa",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.Phongs.Remove(phongGoc);
                _db.SaveChanges();
                LocDanhSachPhong();
                LamMoi();
                MessageBox.Show("Đã xóa phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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