<div align="center">

# 🏛️ NHÀ TRỌ 8386 BÌNH ĐỊNH
### Giải Pháp Quản Trị Lưu Trú, Vận Hành Phòng Trọ & Tự Động Hóa Tài Chính

[![.NET 10.0](https://img.shields.io/badge/.NET%2010.0-WPF%20Desktop-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Pattern-MVVM%20Toolkit-059669?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
[![ORM](https://img.shields.io/badge/EF%20Core%2010-Code%20First-D97706?style=flat-square)](https://learn.microsoft.com/en-us/ef/core/)
[![Database](https://img.shields.io/badge/SQLite-Embedded%20DB-DC2626?style=flat-square&logo=sqlite)](https://www.sqlite.org/)
[![UI/UX](https://img.shields.io/badge/UI%20Animation-CubicEase%20%26%203D%20Shadow-2563EB?style=flat-square)](#)

<p align="center">
  <b><a href="#-tổng-quan--điểm-nhấn-công-nghệ">Điểm nhấn</a></b> •
  <b><a href="#-luồng-vận-hành-khép-kín">Luồng nghiệp vụ</a></b> •
  <b><a href="#-chi-tiết-các-phân-hệ-chức-năng">Phân hệ chức năng</a></b> •
  <b><a href="#-công-thức--quy-tắc-tính-toán">Công thức tính</a></b> •
  <b><a href="#-kiến-trúc-mã-nguồn">Cấu trúc dự án</a></b> •
  <b><a href="#-hướng-dẫn-triển-khai">Khởi chạy nhanh</a></b>
</p>

</div>

---

## 🌟 Tổng Quan & Điểm Nhấn Công Nghệ

**Nhà Trọ 8386 Bình Định** là phần mềm Desktop chuyên dụng giúp chủ nhà trọ số hóa toàn diện quy trình quản lý từ khâu đón khách, ký hợp đồng, ghi chỉ số điện nước đến phát hành hóa đơn và thống kê lợi nhuận. Thay vì sử dụng các bảng biểu truyền thống khô khan, hệ thống tập trung vào trải nghiệm tương tác thị giác và tự động hóa số liệu:

> 🎨 **Giao diện Sơ đồ phòng & Hiệu ứng Chuyển động Mượt mà (Interactive UI & Micro-Animations)**  
> Trực quan hóa toàn bộ tòa nhà thành lưới thẻ phòng tự động biến đổi màu sắc theo trạng thái thực tế (`DataTrigger`). Tích hợp hiệu ứng vật lý đàn hồi (`ScaleTransform`, `CubicEase`, `BackEase`) và đổ bóng chiều sâu (`DropShadowEffect`) khi rê chuột hoặc nhấp chọn.

> ⚡ **Bộ máy Tính tiền Lũy kế & Xuất Phiếu In PDF Sắc nét**  
> Tự động kế thừa chỉ số điện nước kỳ trước, tính tiền phòng theo số ngày ở thực tế (pro-rata), gom công nợ cũ và kết xuất phiếu báo tiền trọ qua trình xem trước `FlowDocumentReader` để in hoặc lưu file PDF chỉ với 1 cú nhấp chuột.

> 🧵 **Tổng hợp Tài chính Đa luồng (`async/await`) & Biểu đồ Đồ họa WPF**  
> Màn hình Dashboard xử lý bất đồng bộ trên luồng nền (`Task.Run`), vẽ trực tiếp các thanh biểu đồ hiệu suất khai thác phòng và tỷ lệ thu hồi nợ mà không làm giật/đơ giao diện người dùng.

> 🛡️ **Cơ sở dữ liệu Độc lập (Zero-Config SQLite) & Sao lưu Nóng**  
> Tự động khởi tạo cấu trúc bảng và nạp dữ liệu mẫu ngay lần chạy đầu tiên. Hỗ trợ xuất báo cáo doanh thu ra **Excel (`.csv` chuẩn UTF-8 BOM)** và sao lưu toàn bộ file CSDL `.db` trực tiếp trên phần mềm.

---

## 🔄 Luồng Vận Hành Khép Kín

```text
┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│  1. SƠ ĐỒ PHÒNG   │ ──► │  2. HỢP ĐỒNG THUÊ │ ──► │ 3. CHỐT ĐIỆN NƯỚC │
│  Chọn phòng trống │     │ Ký mới & Thu cọc  │     │ Kế thừa chỉ số cũ │
└───────────────────┘     └───────────────────┘     └─────────┬─────────┘
                                                              │
┌───────────────────┐     ┌───────────────────┐               ▼
│ 6. DASHBOARD & DB │ ◄── │ 5. THU TIỀN & PDF │ ◄── ┌───────────────────┐
│ Xuất Excel/Backup │     │ Trả góp / In phiếu│     │ 4. LẬP HÓA ĐƠN    │
└───────────────────┘     └───────────────────┘     │ Tính tiền tự động │
                                                    └───────────────────┘