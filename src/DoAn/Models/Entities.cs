using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAn.Models
{
    // 1. Bảng Người Dùng & Cấu hình nhà trọ
    public class NguoiDung
    {
        public int Id { get; set; }
        [Required] public string TenDangNhap { get; set; } = string.Empty;
        [Required] public string MatKhauHash { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string TenNhaTro { get; set; } = "Nhà Trọ Xanh";
        public string DiaChiNhaTro { get; set; } = "TP. Hồ Chí Minh";
        public decimal DonGiaDienMacDinh { get; set; } = 3500;
        public decimal DonGiaNuocMacDinh { get; set; } = 20000;
    }

    // 2. Bảng Loại Phòng
    public class LoaiPhong
    {
        public int Id { get; set; }
        [Required] public string TenLoai { get; set; } = string.Empty;
        public decimal DonGiaCoBan { get; set; }
        public double DienTich { get; set; }
        public int SoNguoiToiDa { get; set; }
        public string MoTaTienNghi { get; set; } = string.Empty;

        public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
    }

    // 3. Bảng Phòng (Hiển thị trên sơ đồ)
    // TrangThai: 0 = Trống, 1 = Đang thuê, 2 = Sắp hết hạn, 3 = Bảo trì
    public class Phong
    {
        public int Id { get; set; }
        [Required] public string MaPhong { get; set; } = string.Empty;
        [Required] public string TenPhong { get; set; } = string.Empty;
        public int Tang { get; set; }
        public int LoaiPhongId { get; set; }
        public LoaiPhong? LoaiPhong { get; set; }
        public decimal GiaThueHienTai { get; set; }
        public int TrangThai { get; set; } // 0: Trống, 1: Đang thuê, 2: Sắp hết hạn, 3: Bảo trì
        public string? GhiChu { get; set; }

        public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
        public ICollection<ChiSoDienNuoc> ChiSoDienNuocs { get; set; } = new List<ChiSoDienNuoc>();
        public ICollection<SuCo> SuCos { get; set; } = new List<SuCo>();
    }

    // 4. Bảng Khách Thuê
    public class KhachThue
    {
        public int Id { get; set; }
        [Required] public string HoTen { get; set; } = string.Empty;
        [Required] public string CCCD { get; set; } = string.Empty;
        [Required] public string SoDienThoai { get; set; } = string.Empty;
        public DateTime NgaySinh { get; set; } = new DateTime(2000, 1, 1);
        public string QueQuan { get; set; } = string.Empty;
        public string? NgheNghiep { get; set; }

        public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
    }

    // 5. Bảng Hợp Đồng
    // TrangThai: 0 = Đang hiệu lực, 1 = Đã kết thúc, 2 = Đã hủy
    public class HopDong
    {
        public int Id { get; set; }
        [Required] public string MaHopDong { get; set; } = string.Empty;
        public int PhongId { get; set; }
        public Phong? Phong { get; set; }
        public int KhachDaiDienId { get; set; }
        public KhachThue? KhachDaiDien { get; set; }

        public DateTime NgayLap { get; set; } = DateTime.Now;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public decimal GiaThueChot { get; set; }
        public decimal TienCoc { get; set; }
        public int SoNguoiO { get; set; } = 1;
        public int SoLuongXe { get; set; } = 1;
        public int TrangThai { get; set; } = 0;
        public decimal? TienHoanCoc { get; set; }
        public DateTime? NgayTraPhongThucTe { get; set; }

        public ICollection<NguoiOCung> NguoiOCungs { get; set; } = new List<NguoiOCung>();
        public ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    }

    // 6. Bảng Người Ở Cùng (Ở ghép)
    public class NguoiOCung
    {
        public int Id { get; set; }
        public int HopDongId { get; set; }
        public HopDong? HopDong { get; set; }
        [Required] public string HoTen { get; set; } = string.Empty;
        public string CCCD { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string QuanHeVoiDaiDien { get; set; } = "Bạn cùng phòng";
    }

    // 7. Bảng Dịch Vụ Phụ (Wifi, Rác, Gửi xe)
    // CachTinh: 0 = Theo phòng, 1 = Theo đầu người, 2 = Theo số lượng xe
    public class DichVu
    {
        public int Id { get; set; }
        [Required] public string TenDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int CachTinh { get; set; }
        public bool BatBuoc { get; set; } = true;
    }

    // 8. Bảng Chỉ Số Điện Nước Hàng Tháng
    public class ChiSoDienNuoc
    {
        public int Id { get; set; }
        public int PhongId { get; set; }
        public Phong? Phong { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int ChiSoDienCu { get; set; }
        public int ChiSoDienMoi { get; set; }
        public int ChiSoNuocCu { get; set; }
        public int ChiSoNuocMoi { get; set; }
        public DateTime NgayChot { get; set; } = DateTime.Now;
    }

    // 9. Bảng Hóa Đơn Hàng Tháng
    // TrangThai: 0 = Chưa thanh toán, 1 = Thanh toán một phần, 2 = Đã thanh toán đủ
    public class HoaDon
    {
        public int Id { get; set; }
        [Required] public string MaHoaDon { get; set; } = string.Empty;
        public int HopDongId { get; set; }
        public HopDong? HopDong { get; set; }
        public int PhongId { get; set; }
        public Phong? Phong { get; set; }

        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienNuoc { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal NoCu { get; set; }
        public decimal TongCong { get; set; }
        public decimal SoTienDaDong { get; set; }
        public decimal ConNo { get; set; }
        public int TrangThai { get; set; } = 0;
        public DateTime NgayLap { get; set; } = DateTime.Now;

        public ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
    }

    // 10. Bảng Lịch Sử Thanh Toán
    public class ThanhToan
    {
        public int Id { get; set; }
        public int HoaDonId { get; set; }
        public HoaDon? HoaDon { get; set; }
        public DateTime NgayThanhToan { get; set; } = DateTime.Now;
        public decimal SoTienThu { get; set; }
        public string PhuongThuc { get; set; } = "Chuyển khoản";
        public string? GhiChu { get; set; }
    }

    // 11. Bảng Sự Cố & Bảo Trì
    // TrangThai: 0 = Mới báo, 1 = Đang sửa, 2 = Hoàn thành
    public class SuCo
    {
        public int Id { get; set; }
        public int PhongId { get; set; }
        public Phong? Phong { get; set; }
        [Required] public string TieuDe { get; set; } = string.Empty;
        public string MoTaChiTiet { get; set; } = string.Empty;
        public DateTime NgayBao { get; set; } = DateTime.Now;
        public decimal ChiPhiSuaChua { get; set; }
        public int TrangThai { get; set; } = 0;
    }
}