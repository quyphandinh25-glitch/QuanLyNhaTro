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
- [Luồng nghiệp vụ chính](#-luồng-nghiệp-vụ-chính)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Lộ trình phát triển (8 Giai đoạn)](#-lộ-trình-phát-triển-8-giai-đoạn)
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

## 🔄 Luồng nghiệp vụ chính

```text
Đăng nhập (Phân quyền)
   │
   ├──► Xem Sơ đồ phòng trực quan ──► Chọn phòng ──► Tạo Hợp đồng / Cọc
   │                                                    │
   ├──► Chốt số Điện / Nước hàng tháng ◄────────────────┘
   │         │
   │         ▼
   ├──► Lập Hóa đơn hàng loạt ──► Thu tiền / Thanh toán ──► Xuất PDF Hóa đơn
   │                                  │
   └──► Báo cáo Doanh thu & Dashboard ◄─┴── Export Excel