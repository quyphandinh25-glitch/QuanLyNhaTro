# Rental Management System — Quản Lý Nhà Trọ

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/Platform-WPF-blue)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-brightgreen)
![Database](https://img.shields.io/badge/DB-SQL_Server-red)

Đồ án môn Lập trình trực quan: Ứng dụng desktop quản lý nhà trọ — quản lý phòng, khách thuê, chốt chỉ số điện nước hàng tháng và xuất hóa đơn.

## Mục lục
- [Tính năng](#tính-năng)
- [Công nghệ](#công-nghệ)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Cài đặt và chạy](#cài-đặt-và-chạy)

## Tính năng

| Nhóm | Nội dung |
| :--- | :--- |
| **Danh mục** | Quản lý phòng trọ, khách thuê, hợp đồng (CRUD + Tìm kiếm) |
| **Trực quan** | Sơ đồ phòng đổi màu theo trạng thái (`Trống`, `Đang thuê`, `Nợ tiền`) |
| **Tính tiền** | Chốt chỉ số điện/nước hàng tháng, tính tiền tự động |
| **Hóa đơn** | Lập hóa đơn thanh toán và xuất file báo cáo |

Luồng nghiệp vụ: `Đăng nhập` -> `Sơ đồ phòng` -> `Tạo hợp đồng` -> `Chốt điện nước` -> `Xuất hóa đơn`.

## Công nghệ

| Lớp | Lựa chọn |
| :--- | :--- |
| **UI** | WPF trên .NET 10.0 |
| **Pattern** | MVVM (`CommunityToolkit.Mvvm`) |
| **Database** | SQL Server + Entity Framework Core |

## Cấu trúc dự án

- `src/`: Mã nguồn chính của dự án C# / WPF.
- `docs/`: Tài liệu báo cáo, sơ đồ CSDL (ERD).