# Rental Management System — Hệ Thống Quản Lý Nhà Trọ / Ký Túc Xá

![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge&logo=.net)
![WPF](https://img.shields.io/badge/Platform-WPF-blue?style=for-the-badge)
![MVVM](https://img.shields.io/badge/Architecture-MVVM-brightgreen?style=for-the-badge)
![EF Core](https://img.shields.io/badge/ORM-EF_Core_10-orange?style=for-the-badge)
![Database](https://img.shields.io/badge/DB-SQL_Server_%2F_SQLite-red?style=for-the-badge)

Ứng dụng Desktop quản lý nhà trọ / ký túc xá toàn diện xây dựng trên nền tảng **.NET 10.0 WPF** theo chuẩn kiến trúc **MVVM**. Hỗ trợ quản lý sơ đồ phòng trực quan, theo dõi hợp đồng, chốt điện nước hàng tháng, tính công nợ, xuất hóa đơn PDF và báo cáo thống kê doanh thu.

---

## 📌 Mục lục
- [Tính năng nổi bật](#-tính-năng-nổi-bật)
- [Quy tắc nghiệp vụ](#-quy-tắc-nghiệp-vụ)
- [Tài khoản demo & Phân quyền](#-tài-khoản-demo--phân-quyền)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Lộ trình phát triển (8 Giai đoạn)](#-lộ-trình-phát-triển-8-giai-đoạn)
- [Tài liệu & Sản phẩm bàn giao](#-tài-liệu--sản-phẩm-bàn-giao)
- [Cài đặt & Chạy dự án](#-cài-đặt--chạy-dự-án)

---

## 🚀 Tính năng nổi bật

| Nhóm chức năng | Chi tiết tính năng |
| :--- | :--- |
| **Sơ đồ phòng (Core UI)** | • Bố cục theo tầng/dãy với `ItemsControl` + `UniformGrid` / `WrapPanel`.<br>• Ô phòng tự động đổi màu theo trạng thái bằng `DataTrigger` (`Trống`, `Đang thuê`, `Sắp hết hạn`, `Bảo trì`).<br>• Kéo thả (`Drag & Drop`) khách/hợp đồng để chuyển phòng nhanh.<br>• Panel thao tác nhanh khi bấm chọn ô phòng (cho thuê, trả phòng, chốt điện nước, báo sự cố).<br>• Chế độ chỉnh sửa sơ đồ kéo thả ô phòng trên `Canvas` và lưu tọa độ vào CSDL. |
| **Quản lý danh mục** | • Danh mục phòng, loại phòng, tiện nghi, đơn giá và trạng thái.<br>• Hồ sơ khách thuê, tìm kiếm và lịch sử thuê phòng. |
| **Hợp đồng thuê** | • Tạo mới hợp đồng (chọn phòng, người ở, tiền cọc).<br>• Xử lý gia hạn, trả phòng, hoàn cọc và tự động cập nhật trạng thái phòng. |
| **Điện nước & Dịch vụ** | • Chốt chỉ số điện/nước hàng tháng (nhập nhanh theo danh sách/sơ đồ, kiểm tra chỉ số mới $\ge$ cũ, cảnh báo bất thường).<br>• Quản lý dịch vụ phụ (internet, rác, gửi xe) theo phòng hoặc đầu người. |
| **Hóa đơn & Thu tiền** | • Lập hóa đơn hàng loạt hàng tháng (tiền phòng + điện nước + dịch vụ + nợ cũ).<br>• Thu tiền & công nợ: hỗ trợ thanh toán đủ / một phần, nhắc nợ quá hạn.<br>• In và xuất hóa đơn, hợp đồng ra file **PDF**. |
| **Báo cáo & Tiện ích** | • **Dashboard:** Biểu đồ & thẻ số liệu về tỷ lệ lấp đầy, doanh thu tháng, tổng nợ, hợp đồng sắp hết hạn.<br>• Báo cáo doanh thu theo tháng/năm, công nợ và xuất dữ liệu ra file **Excel**.<br>• Báo cáo & theo dõi trạng thái xử lý sự cố / bảo trì.<br>• Cài đặt đơn giá, thông tin nhà trọ, sao lưu & khôi phục dữ liệu. |

---

## ⚙️ Quy tắc nghiệp vụ

* **Trạng thái phòng:** Tự động chuyển đổi giữa các trạng thái `Trống` (Xanh lá), `Đang thuê` (Xanh dương), `Sắp hết hạn hợp đồng` (Vàng), và `Bảo trì / Sự cố` (Đỏ).
* **Tính tiền phòng vào/ra giữa tháng:** Tự động quy đổi tiền phòng theo số ngày thực tế ở khi khách bắt đầu thuê hoặc trả phòng không trọn tháng.
* **Quy trình cọc & Hoàn cọc:** Ghi nhận tiền cọc khi ký hợp đồng, tự động khấu trừ công nợ hoặc phí hư hại tài sản khi chấm dứt hợp đồng để tính số tiền hoàn cọc thực tế.
* **Chốt điện nước:** Bắt buộc chỉ số mới $\ge$ chỉ số cũ. Hệ thống tự động cảnh báo nếu lượng tiêu thụ vượt mức trung bình 200%.

---

## 🔑 Tài khoản demo & Phân quyền

| Tài khoản | Mật khẩu | Vai trò | Quyền hạn |
| :--- | :--- | :--- | :--- |
| `admin` | `admin123` | **Quản trị viên** | Full quyền: Quản lý phòng, hợp đồng, cài đặt đơn giá, xem báo cáo doanh thu & sao lưu CSDL |
| `staff` | `staff123` | **Nhân viên** | Lập hóa đơn, chốt chỉ số điện nước, thu tiền công nợ, tiếp nhận báo sự cố |

*(Lưu ý: Mật khẩu trong CSDL được mã hóa an toàn bằng thuật toán Băm Mật Khẩu)*

---

## 🛠 Công nghệ sử dụng

| Lớp (Layer) | Công nghệ & Thư viện |
| :--- | :--- |
| **Framework** | **.NET 10.0** (Windows WPF) |
| **Kiến trúc UI** | **MVVM Pattern** (`CommunityToolkit.Mvvm`), Dependency Injection |
| **Database & ORM** | **Entity Framework Core 10**, SQLite / SQL Server LocalDB, Fluent API, Migrations |
| **Thiết kế Pattern** | Repository & Service Pattern |
| **Báo cáo & Export** | Thư viện Xuất file PDF (Hóa đơn/Hợp đồng) & Excel (Báo cáo doanh thu) |
| **Kiểm thử** | Unit Test (Logic tính tiền, chốt điện nước, hoàn cọc) & Test Case thủ công |

---

## 📁 Cấu trúc dự án

```text
QuanLyNhaTro/
├── docs/                      # Sơ đồ ERD, Use Case, Figma Design, Báo cáo Word
└── src/                       # Mã nguồn chính dự án
    ├── QuanLyNhaTro.sln       # Visual Studio Solution (.NET 10)
    └── QuanLyNhaTro/
        ├── Configurations/    # Cấu hình App, Dependency Injection
        ├── Data/              # DbContext, Migrations, Seed Data (EF Core)
        ├── Models/            # Entity Models (Phong, KhachThue, HopDong, HoaDon...)
        ├── Repositories/      # Tầng truy xuất dữ liệu (Repository Pattern)
        ├── Services/          # Tầng xử lý logic nghiệp vụ (Service Pattern)
        ├── ViewModels/        # ViewModels điều hướng & Binding dữ liệu
        ├── Views/             # Giao diện XAML (MainWindow, Sidebar, Controls)
        ├── Converters/        # ValueConverters cho XAML Binding
        └── Helpers/           # PDF/Excel Exporter, Password Hasher, Validation