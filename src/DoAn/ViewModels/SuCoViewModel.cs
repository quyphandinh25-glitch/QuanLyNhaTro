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
    public class TrangThaiSuCoOption
    {
        public int Ma { get; set; }
        public string Ten { get; set; } = string.Empty;
    }

    public partial class SuCoViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        // Giữ nguyên các [ObservableProperty] gốc để tránh xung đột Source Generator
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

        public List<TrangThaiSuCoOption> DanhSachTrangThaiForm { get; } = new()
        {
            new TrangThaiSuCoOption { Ma = 0, Ten = "Mới tiếp nhận" },
            new TrangThaiSuCoOption { Ma = 1, Ten = "Đang sửa chữa" },
            new TrangThaiSuCoOption { Ma = 2, Ten = "Đã hoàn thành" }
        };

        [ObservableProperty] private string _locTrangThaiChon = "Tất cả sự cố";

        // Các trường nhập liệu gốc
        [ObservableProperty] private Phong? _phongChon;
        [ObservableProperty] private string _tieuDe = string.Empty;
        [ObservableProperty] private string _moTaChiTiet = string.Empty;
        [ObservableProperty] private decimal _chiPhiSuaChua;
        [ObservableProperty] private int _trangThai = 0;
        [ObservableProperty] private bool _chuyenPhongSangBaoTri = true;

        // Thống kê chi phí bảo trì gốc
        [ObservableProperty] private decimal _tongChiPhiSuaChua;
        [ObservableProperty] private int _soSuCoDangXuLy;

        // Các thuộc tính mới khai báo tường minh để không bị CS0103
        private string _tuKhoaTimKiem = string.Empty;
        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set
            {
                if (SetProperty(ref _tuKhoaTimKiem, value))
                    TaiDanhSachSuCo();
            }
        }

        private int _soSuCoDaHoanThanh;
        public int SoSuCoDaHoanThanh
        {
            get => _soSuCoDaHoanThanh;
            set => SetProperty(ref _soSuCoDaHoanThanh, value);
        }

        private string _soLuongHienThiText = "0 sự cố";
        public string SoLuongHienThiText
        {
            get => _soLuongHienThiText;
            set => SetProperty(ref _soLuongHienThiText, value);
        }

        private bool _coTheHoanTat;
        public bool CoTheHoanTat
        {
            get => _coTheHoanTat;
            set => SetProperty(ref _coTheHoanTat, value);
        }

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
                MoTaChiTiet = value.MoTaChiTiet ?? string.Empty;
                ChiPhiSuaChua = value.ChiPhiSuaChua;
                TrangThai = value.TrangThai;
                ChuyenPhongSangBaoTri = PhongChon?.TrangThai == 3;
                CoTheHoanTat = value.TrangThai != 2;
            }
            else
            {
                CoTheHoanTat = false;
            }
        }

        public void TaiDuLieu()
        {
            DanhSachPhong = new ObservableCollection<Phong>(_db.Phongs.OrderBy(p => p.MaPhong).ToList());
            if (PhongChon == null)
            {
                PhongChon = DanhSachPhong.FirstOrDefault();
            }
            TaiDanhSachSuCo();
        }

        private void TaiDanhSachSuCo()
        {
            var tatCa = _db.SuCos
                .Include(s => s.Phong)
                .OrderBy(s => s.TrangThai == 2 ? 1 : 0) // Ưu tiên sự cố chưa xong lên trên
                .ThenByDescending(s => s.NgayBao)
                .ToList();

            TongChiPhiSuaChua = tatCa.Sum(s => s.ChiPhiSuaChua);
            SoSuCoDangXuLy = tatCa.Count(s => s.TrangThai != 2);
            SoSuCoDaHoanThanh = tatCa.Count(s => s.TrangThai == 2);

            var ketQua = tatCa.AsEnumerable();
            if (LocTrangThaiChon.StartsWith("0")) ketQua = ketQua.Where(s => s.TrangThai == 0);
            else if (LocTrangThaiChon.StartsWith("1")) ketQua = ketQua.Where(s => s.TrangThai == 1);
            else if (LocTrangThaiChon.StartsWith("2")) ketQua = ketQua.Where(s => s.TrangThai == 2);

            if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
            {
                string kw = TuKhoaTimKiem.Trim().ToLower();
                ketQua = ketQua.Where(s =>
                    (s.Phong?.MaPhong ?? "").ToLower().Contains(kw) ||
                    (s.Phong?.TenPhong ?? "").ToLower().Contains(kw) ||
                    (s.TieuDe ?? "").ToLower().Contains(kw) ||
                    (s.MoTaChiTiet ?? "").ToLower().Contains(kw));
            }

            var dsLoc = ketQua.ToList();
            DanhSachSuCo = new ObservableCollection<SuCo>(dsLoc);
            SoLuongHienThiText = $"Hiển thị {dsLoc.Count} / {tatCa.Count} sự cố";
        }

        // Hàm dùng chung khôi phục trạng thái phòng từ Bảo trì (3) về đúng trạng thái theo Hợp đồng
        private bool KhoiPhucTrangThaiPhongNeuHetSuCo(int phongId, int? boQuaSuCoId = null)
        {
            var phong = _db.Phongs.FirstOrDefault(p => p.Id == phongId);
            if (phong == null || phong.TrangThai != 3) return false;

            bool conSuCoChuaXong = _db.SuCos.Any(s =>
                s.PhongId == phongId &&
                s.TrangThai != 2 &&
                (!boQuaSuCoId.HasValue || s.Id != boQuaSuCoId.Value));

            if (conSuCoChuaXong) return false;

            var hdHieuLuc = _db.HopDongs.FirstOrDefault(h => h.PhongId == phongId && h.TrangThai == 0);
            if (hdHieuLuc != null)
            {
                double soNgayConLai = (hdHieuLuc.NgayKetThuc.Date - DateTime.Today).TotalDays;
                phong.TrangThai = soNgayConLai <= 15 ? 2 : 1;
            }
            else
            {
                phong.TrangThai = 0;
            }

            phong.GhiChu = "Đã hoàn tất sửa chữa";
            return true;
        }

        [RelayCommand]
        private void ThemSuCo()
        {
            if (PhongChon == null || string.IsNullOrWhiteSpace(TieuDe))
            {
                MessageBox.Show("Vui lòng chọn Phòng và nhập Tiêu đề sự cố!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ChiPhiSuaChua < 0)
            {
                MessageBox.Show("Chi phí sửa chữa không được là số âm!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (ChuyenPhongSangBaoTri && TrangThai != 2)
            {
                PhongChon.TrangThai = 3; // 3 = Đang bảo trì
                PhongChon.GhiChu = TieuDe.Trim();
            }

            _db.SuCos.Add(suCoMoi);
            _db.SaveChanges();

            string maPhong = PhongChon.MaPhong;
            TaiDanhSachSuCo();
            LamMoi();
            MessageBox.Show($"Đã ghi nhận sự cố bảo trì cho phòng {maPhong} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void CapNhatSuCo()
        {
            if (SuCoDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một sự cố trên bảng để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PhongChon == null || string.IsNullOrWhiteSpace(TieuDe))
            {
                MessageBox.Show("Phòng và Tiêu đề sự cố không được để trống!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ChiPhiSuaChua < 0)
            {
                MessageBox.Show("Chi phí sửa chữa không được là số âm!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int idVuaSua = SuCoDangChon.Id;
            int phongIdCu = SuCoDangChon.PhongId;

            SuCoDangChon.PhongId = PhongChon.Id;
            SuCoDangChon.TieuDe = TieuDe.Trim();
            SuCoDangChon.MoTaChiTiet = MoTaChiTiet.Trim();
            SuCoDangChon.ChiPhiSuaChua = ChiPhiSuaChua;
            SuCoDangChon.TrangThai = TrangThai;

            // Đồng bộ trạng thái phòng mới và phòng cũ (nếu đổi phòng hoặc đổi sang Hoàn thành)
            if (TrangThai == 2)
            {
                KhoiPhucTrangThaiPhongNeuHetSuCo(PhongChon.Id, boQuaSuCoId: idVuaSua);
            }
            else if (ChuyenPhongSangBaoTri)
            {
                PhongChon.TrangThai = 3;
                PhongChon.GhiChu = TieuDe.Trim();
            }
            else if (PhongChon.TrangThai == 3)
            {
                KhoiPhucTrangThaiPhongNeuHetSuCo(PhongChon.Id, boQuaSuCoId: idVuaSua);
            }

            if (phongIdCu != PhongChon.Id)
            {
                KhoiPhucTrangThaiPhongNeuHetSuCo(phongIdCu, boQuaSuCoId: idVuaSua);
            }

            _db.SaveChanges();
            TaiDanhSachSuCo();
            SuCoDangChon = DanhSachSuCo.FirstOrDefault(s => s.Id == idVuaSua);

            MessageBox.Show("Đã cập nhật thông tin sự cố và đồng bộ trạng thái phòng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void HoanTatSuaChua()
        {
            if (SuCoDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn một sự cố trên bảng để đánh dấu hoàn thành!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SuCoDangChon.TrangThai == 2)
            {
                MessageBox.Show("Sự cố này đã được đánh dấu hoàn thành trước đó!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var suCo = SuCoDangChon;
            if (MessageBox.Show(
                $"Xác nhận đã sửa chữa xong sự cố '{suCo.TieuDe}' tại phòng {suCo.Phong?.MaPhong}?\n" +
                $"• Chi phí sửa chữa ghi nhận: {suCo.ChiPhiSuaChua:N0} VNĐ",
                "Xác nhận hoàn tất bảo trì", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            int idSuCo = suCo.Id;
            int phongId = suCo.PhongId;
            string maPhong = suCo.Phong?.MaPhong ?? "";

            suCo.TrangThai = 2; // 2 = Đã hoàn thành
            bool daMoLaiPhong = KhoiPhucTrangThaiPhongNeuHetSuCo(phongId, boQuaSuCoId: idSuCo);

            _db.SaveChanges();
            TaiDanhSachSuCo();
            SuCoDangChon = DanhSachSuCo.FirstOrDefault(s => s.Id == idSuCo);

            string thongBao = daMoLaiPhong
                ? $"Đã hoàn tất sửa chữa và khôi phục trạng thái hoạt động cho phòng {maPhong}!"
                : $"Đã đánh dấu hoàn tất sự cố tại phòng {maPhong}!\n(Lưu ý: Nếu phòng vẫn ở trạng thái Bảo trì, vui lòng kiểm tra các sự cố khác chưa xong của phòng này.)";

            MessageBox.Show(thongBao, "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void XoaSuCo()
        {
            if (SuCoDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn sự cố cần xóa trên bảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bản ghi sự cố '{SuCoDangChon.TieuDe}' (Phòng {SuCoDangChon.Phong?.MaPhong})?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int idXoa = SuCoDangChon.Id;
                int phongId = SuCoDangChon.PhongId;

                _db.SuCos.Remove(SuCoDangChon);
                KhoiPhucTrangThaiPhongNeuHetSuCo(phongId, boQuaSuCoId: idXoa);

                _db.SaveChanges();
                TaiDanhSachSuCo();
                LamMoi();
                MessageBox.Show("Đã xóa bản ghi sự cố thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void LamMoi()
        {
            SuCoDangChon = null;
            PhongChon = DanhSachPhong.FirstOrDefault();
            TieuDe = string.Empty;
            MoTaChiTiet = string.Empty;
            ChiPhiSuaChua = 0;
            TrangThai = 0;
            ChuyenPhongSangBaoTri = true;
        }
    }
}