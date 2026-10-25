<div align="center">

# 🏛️ NHÀ TRỌ 8386 BÌNH ĐỊNH
### Giải Pháp Quản Trị Lưu Trú, Vận Hành Phòng Trọ & Tự Động Hóa Tài Chính

[![.NET 10.0](https://img.shields.io/badge/.NET%2010.0-WPF%20Desktop-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Pattern-MVVM%20Toolkit-059669?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
[![ORM](https://img.shields.io/badge/EF%20Core%2010-Code%20First-D97706?style=flat-square)](https://learn.microsoft.com/en-us/ef/core/)
[![Database](https://img.shields.io/badge/SQLite-Embedded%20DB-DC2626?style=flat-square&logo=sqlite)](https://www.sqlite.org/)
[![UI/UX](https://img.shields.io/badge/UI%20Animation-CubicEase%20%26%203D%20Shadow-2563EB?style=flat-square)](#)

<p align="center">
  <b><a href="#diem-nhan">Điểm nhấn</a></b> •
  <b><a href="#luong-nghiep-vu">Luồng nghiệp vụ</a></b> •
  <b><a href="#phan-he-chuc-nang">Phân hệ chức năng</a></b> •
  <b><a href="#cong-thuc-tinh">Công thức tính</a></b> •
  <b><a href="#cau-truc-du-an">Cấu trúc dự án</a></b> •
  <b><a href="#khoi-chay-nhanh">Khởi chạy nhanh</a></b>
</p>

</div>

---

<a id="diem-nhan"></a>
## 🌟 1. Tổng Quan & Điểm Nhấn Công Nghệ

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

<a id="luong-nghiep-vu"></a>
## 🔄 2. Luồng Vận Hành Khép Kín

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
```

---

<a id="phan-he-chuc-nang"></a>
## 🧩 3. Chi Tiết Các Phân Hệ Chức Năng

### Ⅰ. Không Gian & Lưu Trú (Space & Tenancy Management)

* **🗺️ Sơ đồ phòng trực quan (Core UI):**
  * Hiển thị thẻ phòng thông minh với 4 mã màu trạng thái tự động: 🟢 **Phòng trống**, 🔵 **Đang thuê**, 🟠 **Sắp hết hạn** (cảnh báo khi hợp đồng còn $\le 15$ ngày) và 🔴 **Đang bảo trì**.
  * Bộ lọc đa chiều theo Tầng, Trạng thái, Từ khóa tìm kiếm kèm bộ đếm số lượng theo thời gian thực.
  * **Panel Thao tác nhanh:** Thực hiện ngay các nghiệp vụ *Cho thuê nhanh (1 năm)*, *Gia hạn nhanh (+6 tháng)*, *Quyết toán trả phòng* hoặc *Báo bảo trì / Mở lại phòng* ngay tại sơ đồ mà không cần chuyển tab.
* **🚪 Quản lý Danh mục Phòng & Khách thuê:**
  * Phân loại phòng (*Phòng đơn, Phòng có gác, Studio*), thiết lập diện tích và đơn giá niêm yết.
  * Quản lý hồ sơ khách thuê (CCCD, SĐT, Quê quán) và kiểm tra ràng buộc toàn vẹn dữ liệu trước khi xóa.
* **📄 Quản lý Vòng đời Hợp đồng:**
  * Tự động điền giá phòng và tiền cọc gợi ý khi lập hợp đồng mới; quản lý số lượng người ở và số xe máy để làm cơ sở tính phí dịch vụ.
  * **Thanh lý & Quyết toán cọc:** Tự động đối trừ tổng công nợ hóa đơn chưa thanh toán vào tiền đặt cọc và giải phóng phòng về trạng thái trống.

### Ⅱ. Tài Chính, Công Nợ & In Ấn (Billing & Invoicing Engine)

* **⚡ Chốt Chỉ số Điện Nước & Dịch vụ phụ:**
  * Tự động lấy **Chỉ số mới** của tháng trước gán làm **Chỉ số cũ** cho kỳ chốt hiện tại.
  * Cho phép nhập nhanh trực tiếp trên lưới `DataGrid`, kiểm tra điều kiện $\text{Chỉ số mới} \ge \text{Chỉ số cũ}$ và bật cảnh báo nếu tiêu thụ vượt ngưỡng bất thường ($> 500\text{ kWh}$ điện hoặc $> 50\text{ m}^3$ nước).
  * Thiết lập linh hoạt đơn giá Điện, Nước và 3 hình thức tính phí dịch vụ (*Wifi, Rác, Gửi xe*): tính theo phòng, theo đầu người hoặc theo số lượng xe.
* **🧾 Phát hành Hóa đơn, Thu nợ & Xuất PDF:**
  * **Lập hóa đơn hàng loạt:** Chỉ với 1 nút bấm, hệ thống quét toàn bộ hợp đồng đang hiệu lực để tổng hợp *Tiền phòng + Tiền điện + Tiền nước + Phí dịch vụ + Nợ cũ kỳ trước*.
  * **Quản lý thu tiền linh hoạt:** Hỗ trợ khách đóng đủ 1 lần hoặc đóng trước một phần (ghi nhận công nợ còn lại), lưu vết lịch sử giao dịch (`ThanhToans`) và lọc nhanh các phòng đang nợ.
  * **Xem trước & Xuất PDF:** Dựng bản in phiếu báo tiền trọ chuẩn chỉnh bằng `FlowDocument` kèm thanh công cụ xuất trực tiếp ra file **PDF**.

### Ⅲ. Kỹ Thuật, Thống Kê & Quản Trị (Maintenance & Analytics)

* **🛠️ Quản lý Sự cố & Bảo trì:**
  * Ghi nhận hư hỏng theo phòng, theo dõi tiến độ (*Mới tiếp nhận ➔ Đang sửa chữa ➔ Đã hoàn thành*) và tổng hợp chi phí sửa chữa.
  * Tự động đồng bộ trạng thái phòng sang *Đang bảo trì* khi có sự cố và tự động khôi phục trạng thái phòng khi hoàn tất sửa chữa.
* **📊 Dashboard Đa luồng, Xuất Excel & Sao lưu CSDL:**
  * Tính toán bất đồng bộ (`async/await`) các chỉ số: Doanh thu dự kiến, Thực thu, Công nợ tồn đọng, Chi phí bảo trì và Lợi nhuận ròng.
  * Biểu đồ thanh đồ họa WPF trực quan hóa tỷ lệ lấp đầy phòng và hiệu suất thu tiền.
  * Xuất bảng kê doanh thu ra file **Excel (`.csv`)** hiển thị chuẩn tiếng Việt và sao lưu nóng file **`QuanLyNhaTro.db`** ra máy tính.

---

<a id="cong-thuc-tinh"></a>
## 🧮 4. Công Thức & Quy Tắc Tính Toán

Hệ thống áp dụng 4 công thức chuẩn hóa tự động hoàn toàn trong tầng `ViewModel`:

1. **Tiền phòng tháng đầu tiên (Áp dụng khi khách dọn vào sau ngày 05 của tháng):**
   $$\text{Tiền phòng} = \text{Round}\left(\frac{\text{Giá thuê chốt}}{30} \times (30 - \text{Ngày vào} + 1)\right)$$

2. **Chi phí dịch vụ phụ hàng tháng:**
   $$\text{Tiền dịch vụ} = \sum \text{DV theo phòng} + (\text{DV đầu người} \times \text{Số người}) + (\text{DV gửi xe} \times \text{Số xe})$$

3. **Tổng cộng Hóa đơn phải thu:**
   $$\text{Tổng phải thu} = \text{Tiền phòng} + (\Delta \text{Điện} \times \text{Giá điện}) + (\Delta \text{Nước} \times \text{Giá nước}) + \text{Tiền dịch vụ} + \text{Nợ cũ}$$

4. **Quyết toán tiền hoàn cọc khi thanh lý hợp đồng:**
   $$\text{Tiền hoàn cọc} = \text{Tiền đặt cọc} - \sum \text{Công nợ hóa đơn chưa đóng}$$

---

<a id="cau-truc-du-an"></a>
## 🏗️ 5. Kiến Trúc Mã Nguồn & Cấu Trúc Dự Án

### Bảng Công Nghệ Áp Dụng

| Thành phần | Công nghệ / Thư viện triển khai |
| :--- | :--- |
| **Nền tảng phát triển** | **.NET 10.0** (Windows Presentation Foundation - WPF) |
| **Kiến trúc & Dependency Injection** | **MVVM** (`CommunityToolkit.Mvvm`), `Microsoft.Extensions.DependencyInjection` |
| **Cơ sở dữ liệu & ORM** | **SQLite** & **Entity Framework Core 10** (`Code-First`, `Auto-Migration & Seeding`) |
| **Kỹ thuật Giao diện & Đồ họa** | `DataTemplate`, `ControlTemplate`, `DataTrigger`, `ScaleTransform`, `CubicEase`, `BackEase`, `DropShadowEffect` |
| **Xử lý Bất đồng bộ** | Đa luồng với `async / await`, `Task.Run`, `Dispatcher.Invoke` |
| **Tài liệu & Báo cáo** | `FlowDocumentReader` & `PrintDialog` (Xuất PDF), `UTF8Encoding` CSV (Xuất Excel) |
| **Bảo mật** | Băm mật khẩu một chiều chuẩn **SHA-256** (`PasswordHelper`) |

### Cây Thư Mục Dự Án

```text
QuanLyNhaTro/
├── README.md                             # Tài liệu tổng quan dự án
└── src/
    ├── QuanLyNhaTro.sln                  # Solution quản lý dự án (.NET 10)
    └── DoAn/                             # Project WPF chính
        ├── Configurations/               # Cấu hình hệ thống mở rộng
        ├── Converters/                   # Bộ chuyển đổi dữ liệu hiển thị XAML (ValueConverters)
        ├── Data/
        │   ├── AppDbContext.cs           # Kết nối EF Core SQLite & Khởi tạo cấu trúc bảng
        │   └── DbSeeder.cs               # Tự động nạp dữ liệu mẫu Nhà Trọ 8386 Bình Định
        ├── Helpers/
        │   └── PasswordHelper.cs         # Thuật toán mã hóa băm mật khẩu chuẩn SHA-256
        ├── Models/
        │   └── Entities.cs               # Định nghĩa 10 thực thể CSDL (Phong, HopDong, HoaDon...)
        ├── Repositories/                 # Tầng giao tiếp truy xuất dữ liệu
        ├── Services/
        │   └── AuthService.cs            # Quản lý xác thực và phiên đăng nhập người dùng
        ├── ViewModels/                   # Tầng xử lý logic nghiệp vụ chuẩn MVVM (9 ViewModels)
        │   ├── MainViewModel.cs          # Điều hướng màn hình trung tâm & trạng thái phiên làm việc
        │   ├── DashboardViewModel.cs     # Thống kê đa luồng, xuất báo cáo Excel & sao lưu CSDL
        │   ├── SoDoPhongViewModel.cs     # Logic sơ đồ phòng trực quan, bộ lọc & thao tác nhanh
        │   ├── PhongViewModel.cs         # Nghiệp vụ thêm, sửa, xóa, tìm kiếm phòng & loại phòng
        │   ├── KhachThueViewModel.cs     # Quản lý hồ sơ khách thuê & kiểm tra ràng buộc dữ liệu
        │   ├── HopDongViewModel.cs       # Quản lý ký hợp đồng mới, gia hạn & thanh lý hoàn cọc
        │   ├── DienNuocViewModel.cs      # Chốt chỉ số điện nước kế thừa & cấu hình đơn giá dịch vụ
        │   ├── HoaDonViewModel.cs        # Tính hóa đơn hàng loạt, thu tiền công nợ & tạo bản in PDF
        │   └── SuCoViewModel.cs          # Quản lý tiến độ xử lý sự cố & đồng bộ trạng thái bảo trì
        ├── Views/                        # Tầng giao diện người dùng XAML & Hiệu ứng chuyển động
        │   ├── LoginWindow.xaml          # Màn hình đăng nhập hệ thống
        │   ├── DashboardView.xaml        # Giao diện bảng điều khiển tổng quan & biểu đồ hiệu suất
        │   ├── SoDoPhongView.xaml        # Giao diện lưới thẻ phòng đổi màu tự động & hiệu ứng nổi 3D
        │   ├── PhongView.xaml            # Giao diện quản lý danh mục phòng trọ
        │   ├── KhachThueView.xaml        # Giao diện quản lý hồ sơ khách thuê
        │   ├── HopDongView.xaml          # Giao diện quản lý vòng đời hợp đồng thuê
        │   ├── DienNuocView.xaml         # Giao diện nhập chỉ số điện nước & bảng giá dịch vụ
        │   ├── HoaDonView.xaml           # Giao diện phát hành hóa đơn, lọc nợ & ghi nhận thanh toán
        │   ├── PreviewHoaDonWindow.xaml  # Cửa sổ xem trước phiếu báo tiền trọ & xuất file PDF
        │   └── SuCoView.xaml             # Giao diện tiếp nhận & cập nhật trạng thái sửa chữa sự cố
        ├── App.xaml / App.xaml.cs        # Khai báo DI Container & Global Animation Styles
        └── MainWindow.xaml               # Khung điều hướng chính (Sidebar + Header + ContentControl)
```

---

<a id="khoi-chay-nhanh"></a>
## 🚀 6. Hướng Dẫn Khởi Chạy Nhanh & Đăng Nhập

### 1. Cài đặt và Khởi chạy
* **Yêu cầu:** Windows 10/11, **Visual Studio 2022 / 2026** (workload *.NET Desktop Development*), **.NET 10.0 SDK**.
* **Thao tác trên Terminal:**
  ```bash
  git clone [https://github.com/quyphandinh25-glitch/QuanLyNhaTro.git](https://github.com/quyphandinh25-glitch/QuanLyNhaTro.git)
  cd QuanLyNhaTro
  start src/QuanLyNhaTro.sln
  ```
* Trong Visual Studio, nhấn **`Ctrl + Shift + B`** để biên dịch, sau đó nhấn **`F5`** để chạy ngay lập tức (File CSDL `QuanLyNhaTro.db` sẽ tự động được sinh ra trong thư mục `bin/Debug/...`).

### 2. Thông tin Đăng nhập Mặc định
Hệ thống khởi tạo sẵn tài khoản Quản trị viên với toàn quyền sử dụng:
* **Tài khoản:** `admin`
* **Mật khẩu:** `123456` *(hoặc `admin123`)*

---

<div align="center">
  <i>Dự án được phát triển và hoàn thiện 100% các giai đoạn theo chuẩn kiến trúc MVVM trên nền tảng .NET WPF.</i>
</div>