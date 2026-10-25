using DoAn.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace DoAn.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<KhachThue> KhachThues { get; set; }
        public DbSet<HopDong> HopDongs { get; set; }
        public DbSet<NguoiOCung> NguoiOCungs { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<ChiSoDienNuoc> ChiSoDienNuocs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<SuCo> SuCos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Lưu file QuanLyNhaTro.db ngay tại thư mục chạy của ứng dụng
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuanLyNhaTro.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        // Cấu hình Fluent API: Ràng buộc Khóa, Index, Unique theo đúng yêu cầu Dòng 14 & 23
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique Index cho Tên đăng nhập, Mã phòng, CCCD, Mã hợp đồng, Mã hóa đơn
            modelBuilder.Entity<NguoiDung>().HasIndex(u => u.TenDangNhap).IsUnique();
            modelBuilder.Entity<Phong>().HasIndex(p => p.MaPhong).IsUnique();
            modelBuilder.Entity<KhachThue>().HasIndex(k => k.CCCD).IsUnique();
            modelBuilder.Entity<KhachThue>().HasIndex(k => k.SoDienThoai);
            modelBuilder.Entity<HopDong>().HasIndex(h => h.MaHopDong).IsUnique();
            modelBuilder.Entity<HoaDon>().HasIndex(b => b.MaHoaDon).IsUnique();

            // Ràng buộc 1 Phòng trong 1 Tháng/Năm chỉ có 1 bản ghi chốt điện nước duy nhất
            modelBuilder.Entity<ChiSoDienNuoc>()
                .HasIndex(c => new { c.PhongId, c.Thang, c.Nam })
                .IsUnique();
        }

        // Hàm tự động tạo Database và nạp dữ liệu mẫu (Seed Data) cho 12 phòng
        public void InitializeAndSeed()
        {
            Database.EnsureCreated();

            // 1. Nạp tài khoản Chủ trọ mặc định (admin / 123456)
            if (!NguoiDungs.Any())
            {
                NguoiDungs.Add(new NguoiDung
                {
                    TenDangNhap = "admin",
                    MatKhauHash = "123456", // Sẽ nâng cấp băm SHA256 ở bước Đăng nhập
                    HoTen = "Chủ Trọ Quản Lý",
                    SoDienThoai = "0909123456",
                    TenNhaTro = "Nhà Trọ 8386 Bình Định",
                    DiaChiNhaTro = "Quận Bình Thạnh, TP. Hồ Chí Minh",
                    DonGiaDienMacDinh = 3500,
                    DonGiaNuocMacDinh = 20000
                });
                SaveChanges();
            }

            // 2. Nạp 3 Dịch vụ mặc định
            if (!DichVus.Any())
            {
                DichVus.AddRange(
                    new DichVu { TenDichVu = "Internet Wifi", DonGia = 100000, CachTinh = 0, BatBuoc = true },
                    new DichVu { TenDichVu = "Vệ sinh & Thu gom rác", DonGia = 30000, CachTinh = 0, BatBuoc = true },
                    new DichVu { TenDichVu = "Giữ xe máy", DonGia = 100000, CachTinh = 2, BatBuoc = true }
                );
                SaveChanges();
            }

            // 3. Nạp 3 Loại phòng & 12 Phòng (Chia làm 3 tầng, đủ 4 màu trạng thái)
            if (!LoaiPhongs.Any())
            {
                var loai1 = new LoaiPhong { TenLoai = "Phòng đơn tiêu chuẩn", DonGiaCoBan = 2200000, DienTich = 18, SoNguoiToiDa = 2, MoTaTienNghi = "Quạt trần, WC riêng, Kệ bếp" };
                var loai2 = new LoaiPhong { TenLoai = "Phòng có gác xép", DonGiaCoBan = 2800000, DienTich = 25, SoNguoiToiDa = 3, MoTaTienNghi = "Máy lạnh, Bình nóng lạnh, Gác lửng" };
                var loai3 = new LoaiPhong { TenLoai = "Phòng Studio cao cấp", DonGiaCoBan = 3500000, DienTich = 30, SoNguoiToiDa = 4, MoTaTienNghi = "Full nội thất: Tủ lạnh, Máy lạnh, Giường nệm" };

                LoaiPhongs.AddRange(loai1, loai2, loai3);
                SaveChanges();

                // Tạo 12 phòng cho 3 tầng (TrangThai: 0=Trống, 1=Đang thuê, 2=Sắp hết hạn, 3=Bảo trì)
                var danhSachPhong = new[]
                {
                    // Tầng 1
                    new Phong { MaPhong = "P101", TenPhong = "Phòng 101", Tang = 1, LoaiPhongId = loai1.Id, GiaThueHienTai = 2200000, TrangThai = 1 },
                    new Phong { MaPhong = "P102", TenPhong = "Phòng 102", Tang = 1, LoaiPhongId = loai1.Id, GiaThueHienTai = 2200000, TrangThai = 1 },
                    new Phong { MaPhong = "P103", TenPhong = "Phòng 103", Tang = 1, LoaiPhongId = loai2.Id, GiaThueHienTai = 2800000, TrangThai = 0 },
                    new Phong { MaPhong = "P104", TenPhong = "Phòng 104", Tang = 1, LoaiPhongId = loai2.Id, GiaThueHienTai = 2800000, TrangThai = 2 },
                    // Tầng 2
                    new Phong { MaPhong = "P201", TenPhong = "Phòng 201", Tang = 2, LoaiPhongId = loai2.Id, GiaThueHienTai = 2800000, TrangThai = 1 },
                    new Phong { MaPhong = "P202", TenPhong = "Phòng 202", Tang = 2, LoaiPhongId = loai2.Id, GiaThueHienTai = 2800000, TrangThai = 1 },
                    new Phong { MaPhong = "P203", TenPhong = "Phòng 203", Tang = 2, LoaiPhongId = loai1.Id, GiaThueHienTai = 2200000, TrangThai = 0 },
                    new Phong { MaPhong = "P204", TenPhong = "Phòng 204", Tang = 2, LoaiPhongId = loai3.Id, GiaThueHienTai = 3500000, TrangThai = 2 },
                    // Tầng 3
                    new Phong { MaPhong = "P301", TenPhong = "Phòng 301", Tang = 3, LoaiPhongId = loai3.Id, GiaThueHienTai = 3500000, TrangThai = 1 },
                    new Phong { MaPhong = "P302", TenPhong = "Phòng 302", Tang = 3, LoaiPhongId = loai3.Id, GiaThueHienTai = 3500000, TrangThai = 1 },
                    new Phong { MaPhong = "P303", TenPhong = "Phòng 303", Tang = 3, LoaiPhongId = loai2.Id, GiaThueHienTai = 2800000, TrangThai = 0 },
                    new Phong { MaPhong = "P304", TenPhong = "Phòng 304", Tang = 3, LoaiPhongId = loai1.Id, GiaThueHienTai = 2200000, TrangThai = 3, GhiChu = "Đang chống thấm WC" }
                };
                Phongs.AddRange(danhSachPhong);
                SaveChanges();

                // 4. Nạp Khách thuê & Hợp đồng mẫu cho các phòng Đang thuê / Sắp hết hạn
                var khach1 = new KhachThue { HoTen = "Nguyễn Văn An", CCCD = "079201001234", SoDienThoai = "0901112221", QueQuan = "Đồng Nai", NgheNghiep = "Nhân viên văn phòng" };
                var khach2 = new KhachThue { HoTen = "Trần Thị Bích", CCCD = "079202005678", SoDienThoai = "0901112222", QueQuan = "Long An", NgheNghiep = "Sinh viên" };
                var khach3 = new KhachThue { HoTen = "Lê Hoàng Nam", CCCD = "079200009999", SoDienThoai = "0901112223", QueQuan = "Tiền Giang", NgheNghiep = "Kỹ sư IT" };
                KhachThues.AddRange(khach1, khach2, khach3);
                SaveChanges();

                var p101 = Phongs.First(p => p.MaPhong == "P101");
                var p104 = Phongs.First(p => p.MaPhong == "P104");
                var p304 = Phongs.First(p => p.MaPhong == "P304");

                HopDongs.AddRange(
                    new HopDong
                    {
                        MaHopDong = "HD-2026-001",
                        PhongId = p101.Id,
                        KhachDaiDienId = khach1.Id,
                        NgayBatDau = DateTime.Now.AddMonths(-5),
                        NgayKetThuc = DateTime.Now.AddMonths(7),
                        GiaThueChot = p101.GiaThueHienTai,
                        TienCoc = p101.GiaThueHienTai,
                        SoNguoiO = 2,
                        SoLuongXe = 2,
                        TrangThai = 0
                    },
                    new HopDong
                    {
                        MaHopDong = "HD-2026-002",
                        PhongId = p104.Id,
                        KhachDaiDienId = khach2.Id,
                        NgayBatDau = DateTime.Now.AddMonths(-11),
                        NgayKetThuc = DateTime.Now.AddDays(7), // Sắp hết hạn sau 7 ngày
                        GiaThueChot = p104.GiaThueHienTai,
                        TienCoc = p104.GiaThueHienTai,
                        SoNguoiO = 1,
                        SoLuongXe = 1,
                        TrangThai = 0
                    }
                );

                // Thêm 1 sự cố mẫu cho phòng P304 đang bảo trì
                SuCos.Add(new SuCo
                {
                    PhongId = p304.Id,
                    TieuDe = "Thấm nước trần nhà vệ sinh",
                    MoTaChiTiet = "Nước rò rỉ từ ống thoát tầng trên, đang gọi thợ xử lý",
                    ChiPhiSuaChua = 450000,
                    TrangThai = 1
                });

                SaveChanges();
            }
        }
    }
}