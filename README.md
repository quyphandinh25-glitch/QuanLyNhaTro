# 🏢 Rental Management System — Hệ Thống Quản Lý Nhà Trọ 8386 Bình Định

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![Platform](https://img.shields.io/badge/PLATFORM-WPF-0078D6?style=for-the-badge&logo=windows)
![Architecture](https://img.shields.io/badge/ARCHITECTURE-MVVM-10B981?style=for-the-badge)
![ORM](https://img.shields.io/badge/ORM-EF%20CORE%2010-F59E0B?style=for-the-badge)
![Database](https://img.shields.io/badge/DB-SQLITE-EF4444?style=for-the-badge&logo=sqlite)

Ứng dụng Desktop quản lý nhà trọ / ký túc xá toàn diện xây dựng trên nền tảng **.NET 10.0 WPF** theo chuẩn kiến trúc **MVVM** (`CommunityToolkit.Mvvm`) kết hợp **Entity Framework Core 10** và **SQLite**. Hệ thống hỗ trợ quản lý sơ đồ phòng trực quan đổi màu tự động, theo dõi vòng đời hợp đồng, chốt chỉ số điện nước kế thừa, phát hành hóa đơn hàng loạt, quản lý công nợ, xem trước & xuất phiếu thu PDF, thống kê đồ họa đa luồng và xuất báo cáo Excel[cite: 17].

---

## 📌 Mục lục

- [Tính năng nổi bật](#-tính-năng-nổi-bật)
- [Quy tắc nghiệp vụ & Công thức tính toán](#-quy-tắc-nghiệp-vụ--công-thức-tính-toán)
- [Tài khoản đăng nhập mặc định](#-tài-khoản-đăng-nhập-mặc-định)
- [Công nghệ & Kỹ thuật WPF sử dụng](#-công-nghệ--kỹ-thuật-wpf-sử-dụng)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Lộ trình phát triển (Hoàn thành 100%)](#-lộ-trình-phát-triển-hoàn-thành-100)
- [Cài đặt & Chạy dự án](#-cài-đặt--chạy-dự-án)

---

## 🚀 Tính năng nổi bật

| Nhóm chức năng | Chi tiết tính năng đã hoàn thiện |
| :--- | :--- |
| **1. Sơ đồ phòng trực quan (Core UI)** | • Hiển thị danh sách phòng dạng lưới thẻ (`ListBox` + `WrapPanel`).<br>• Ô phòng tự động đổi màu nền và viền theo trạng thái bằng `DataTrigger`: **🟢 Phòng trống**, **🔵 Đang thuê**, **🟠 Sắp hết hạn**, **🔴 Đang bảo trì**.<br>• Tự động rà soát hợp đồng còn $\le 15$ ngày để chuyển sang trạng thái cảnh báo *Sắp hết hạn*.<br>• Bộ lọc đa tiêu chí theo Tầng, Trạng thái, Từ khóa tìm kiếm kèm huy hiệu đếm số lượng trực quan.<br>• **Panel Thao tác nhanh**: Cho thuê nhanh (ký HĐ 1 năm), Gia hạn nhanh ($+6$ tháng), Trả phòng & quyết toán cọc nhanh, Báo sự cố / Mở lại phòng bảo trì ngay trên sơ đồ[cite: 17]. |
| **2. Quản lý Danh mục (Phòng & Khách thuê)** | • **Quản lý Phòng**: Thêm, sửa, xóa, tìm kiếm phòng trọ; phân loại phòng (*Phòng đơn, Phòng có gác, Studio*), thiết lập giá niêm yết và diện tích.<br>• **Quản lý Khách thuê**: Lưu trữ hồ sơ khách thuê (Họ tên, CCCD, Số điện thoại, Quê quán), tra cứu nhanh và kiểm tra ràng buộc dữ liệu trước khi xóa[cite: 17]. |
| **3. Quản lý Hợp đồng thuê** | • Lập hợp đồng thuê mới cho các phòng trống, tự động gợi ý giá thuê và tiền đặt cọc.<br>• Gia hạn thời gian thuê và cập nhật số người ở, số lượng xe máy.<br>• **Quyết toán & Thanh lý hợp đồng**: Tự động tổng hợp công nợ hóa đơn chưa đóng, tính số tiền cọc hoàn trả cho khách và tự động giải phóng phòng về trạng thái *Phòng trống*[cite: 17]. |
| **4. Chốt Điện Nước & Dịch vụ phụ** | • Tự động kế thừa **Chỉ số mới** của tháng trước làm **Chỉ số cũ** của tháng hiện tại.<br>• Nhập liệu trực tiếp trên bảng `DataGrid` và tự động tính sản lượng tiêu thụ tức thời.<br>• Kiểm tra ràng buộc $\text{Chỉ số mới} \ge \text{Chỉ số cũ}$ và hiển thị hộp thoại cảnh báo khi sản lượng tiêu thụ cao bất thường ($> 500\text{ kWh}$ điện hoặc $> 50\text{ m}^3$ nước).<br>• Cấu hình linh hoạt đơn giá Điện, Nước và các Dịch vụ phụ (*Wifi, Rác, Gửi xe*) theo 3 hình thức: theo phòng, theo đầu người hoặc theo số lượng xe[cite: 17]. |
| **5. Hóa đơn, Thu tiền & Xuất PDF** | • **Tính & Lập hóa đơn hàng loạt**: Tự động quét toàn bộ hợp đồng đang hiệu lực để tổng hợp Tiền phòng + Tiền điện + Tiền nước + Phí dịch vụ + Nợ cũ kỳ trước.<br>• Hỗ trợ tính tiền phòng theo số ngày ở thực tế (làm tròn đến hàng nghìn) nếu khách mới dọn vào ở sau ngày 05 của tháng.<br>• **Ghi nhận thu tiền & Công nợ**: Hỗ trợ thanh toán đủ hoặc thanh toán từng phần, tự động cập nhật số dư còn nợ, lưu lịch sử giao dịch (`ThanhToans`) và lọc danh sách phòng đang nợ.<br>• **Xem trước & Xuất PDF**: Tích hợp cửa sổ xem trước phiếu báo tiền trọ bằng `FlowDocumentReader` và kết xuất trực tiếp ra file **PDF** sắc nét qua `PrintDialog`[cite: 17]. |
| **6. Quản lý Sự cố & Bảo trì** | • Tiếp nhận thông tin hư hỏng tại từng phòng, theo dõi trạng thái xử lý (*Mới tiếp nhận, Đang sửa chữa, Đã hoàn thành*) và chi phí sửa chữa.<br>• Tự động đồng bộ chuyển phòng sang trạng thái *Đang bảo trì* và tự động khôi phục trạng thái hoạt động của phòng khi đánh dấu hoàn tất sửa chữa[cite: 17]. |
| **7. Tổng quan (Dashboard), Đồ họa & Sao lưu** | • **Đa luồng (`async/await` & `Task.Run`)**: Tổng hợp bất đồng bộ các chỉ số doanh thu dự kiến, thực thu, công nợ tồn đọng, chi phí bảo trì và lợi nhuận ròng.<br>• **Đồ họa trực quan**: Biểu đồ thanh hiển thị tỷ lệ lấp đầy phòng, tỷ lệ thực thu và tỷ lệ công nợ.<br>• **Xuất Excel & Backup CSDL**: Xuất báo cáo doanh thu tổng hợp ra file **Excel (`.csv` chuẩn UTF-8 BOM)** không lỗi font tiếng Việt và sao lưu nóng toàn bộ cơ sở dữ liệu SQLite (`QuanLyNhaTro.db`)[cite: 17]. |

---

## 📐 Quy tắc nghiệp vụ & Công thức tính toán

1. **Công thức tính Tiền phòng tháng đầu (nếu khách vào ở sau ngày 05):**
   $$\text{Tiền phòng} = \text{Round}\left(\frac{\text{Giá thuê chốt}}{30} \times (30 - \text{Ngày vào} + 1)\right)$$
[cite: 17]

2. **Công thức tính Tổng cộng Hóa đơn hàng tháng:**
   $$\text{Tổng cộng} = \text{Tiền phòng} + (\Delta \text{Điện} \times \text{Giá điện}) + (\Delta \text{Nước} \times \text{Giá nước}) + \text{Tiền dịch vụ} + \text{Nợ cũ}$$
[cite: 17]

3. **Công thức tính Tiền dịch vụ phụ:**
   $$\text{Tiền dịch vụ} = \sum \text{DV theo phòng} + (\text{DV đầu người} \times \text{Số người ở}) + (\text{DV gửi xe} \times \text{Số lượng xe})$$
[cite: 17]

4. **Công thức Quyết toán hoàn cọc khi trả phòng:**
   $$\text{Tiền hoàn cọc} = \text{Tiền đặt cọc} - \sum \text{Công nợ hóa đơn chưa thanh toán}$$
[cite: 17]

---

## 🔑 Tài khoản đăng nhập mặc định

Khi chạy ứng dụng lần đầu tiên, cơ sở dữ liệu SQLite (`QuanLyNhaTro.db`) cùng dữ liệu mẫu của **Nhà Trọ 8386 Bình Định** sẽ được tự động khởi tạo (`Seed Data`) với thông tin đăng nhập sau[cite: 17]:

| Tài khoản | Mật khẩu | Vai trò | Quyền hạn |
| :--- | :--- | :--- | :--- |
| `admin` | `123456` *(hoặc `admin123`)* | **Chủ trọ Quản lý** | Toàn quyền quản lý sơ đồ phòng, phòng trọ, khách thuê, hợp đồng, chốt điện nước, lập hóa đơn, thu tiền, xuất PDF/Excel và sao lưu CSDL[cite: 17]. |

*(Lưu ý: Hệ thống hỗ trợ xác thực mật khẩu an toàn thông qua `PasswordHelper` với thuật toán băm SHA-256)*[cite: 17].

---

## 🛠️ Công nghệ & Kỹ thuật WPF sử dụng

| Tầng / Hạng mục | Công nghệ & Kỹ thuật chi tiết |
| :--- | :--- |
| **Framework** | **.NET 10.0** (Windows Presentation Foundation - WPF)[cite: 17] |
| **Kiến trúc & DI** | **MVVM Pattern** (`CommunityToolkit.Mvvm`), Dependency Injection (`Microsoft.Extensions.DependencyInjection`)[cite: 17] |
| **Cơ sở dữ liệu & ORM** | **Entity Framework Core 10**, **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`), Code-First & Auto Seeding[cite: 17] |
| **Data Binding & Command** | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `ObservableCollection<T>`, `UpdateSourceTrigger=PropertyChanged`[cite: 17] |
| **Template & Đồ họa UI** | `DataTemplate` (ánh xạ tự động View - ViewModel), `ControlTemplate`, `DataTrigger` (đổi màu động), Biểu đồ thanh đồ họa WPF[cite: 17] |
| **Đa luồng (Multithreading)** | Lập trình bất đồng bộ với `async / await`, `Task.Run` và `Application.Current.Dispatcher.Invoke` trên Dashboard[cite: 17] |
| **In ấn & Xuất báo cáo** | `FlowDocument`, `FlowDocumentReader`, `PrintDialog` (Xuất PDF), `SaveFileDialog` & `UTF8Encoding` (Xuất Excel CSV & Sao lưu SQLite)[cite: 17] |

---

## 📁 Cấu trúc dự án

```text
QuanLyNhaTro/
├── docs/                                 # Tài liệu phân tích, sơ đồ thiết kế & Báo cáo đồ án
├── README.md                             # Tài liệu hướng dẫn và mô tả tổng quan dự án
└── src/
    ├── QuanLyNhaTro.sln                  # Visual Studio Solution
    └── DoAn/                             # Project chính của ứng dụng WPF
        ├── Configurations/               # Cấu hình hệ thống mở rộng
        ├── Converters/                   # Các bộ chuyển đổi dữ liệu hiển thị XAML
        ├── Data/
        │   ├── AppDbContext.cs           # Cấu hình EF Core SQLite & Khởi tạo dữ liệu mẫu
        │   └── DbSeeder.cs               # Dữ liệu mẫu ban đầu (12 phòng, khách thuê, hợp đồng)
        ├── Helpers/
        │   └── PasswordHelper.cs         # Mã hóa băm mật khẩu chuẩn SHA-256
        ├── Models/
        │   └── Entities.cs               # Các thực thể CSDL (Phong, LoaiPhong, KhachThue, HopDong, ChiSoDienNuoc, DichVu, HoaDon, ThanhToan, SuCo, NguoiDung)
        ├── Repositories/                 # Tầng giao tiếp truy xuất dữ liệu
        ├── Services/
        │   └── AuthService.cs            # Dịch vụ xác thực & quản lý phiên đăng nhập
        ├── ViewModels/                   # Tầng xử lý logic hiển thị theo chuẩn MVVM
        │   ├── MainViewModel.cs          # Điều hướng màn hình trung tâm
        │   ├── DashboardViewModel.cs     # Thống kê đa luồng, xuất Excel & sao lưu CSDL
        │   ├── SoDoPhongViewModel.cs     # Logic sơ đồ phòng trực quan & thao tác nhanh
        │   ├── PhongViewModel.cs         # CRUD Quản lý phòng trọ
        │   ├── KhachThueViewModel.cs     # CRUD Quản lý khách thuê
        │   ├── HopDongViewModel.cs       # Quản lý hợp đồng, gia hạn & trả phòng
        │   ├── DienNuocViewModel.cs      # Chốt chỉ số điện nước & cấu hình đơn giá dịch vụ
        │   ├── HoaDonViewModel.cs        # Tính hóa đơn hàng loạt, thu nợ & tạo tài liệu PDF
        │   └── SuCoViewModel.cs          # Quản lý sự cố & đồng bộ trạng thái bảo trì
        ├── Views/                        # Tầng giao diện người dùng (XAML Windows & UserControls)
        │   ├── LoginWindow.xaml          # Màn hình đăng nhập hệ thống
        │   ├── DashboardView.xaml        # Giao diện tổng quan & biểu đồ hiệu suất
        │   ├── SoDoPhongView.xaml        # Giao diện lưới sơ đồ phòng đổi màu tự động
        │   ├── PhongView.xaml            # Giao diện quản lý danh mục phòng
        │   ├── KhachThueView.xaml        # Giao diện quản lý hồ sơ khách thuê
        │   ├── HopDongView.xaml          # Giao diện quản lý hợp đồng thuê
        │   ├── DienNuocView.xaml         # Giao diện chốt chỉ số điện nước & bảng giá
        │   ├── HoaDonView.xaml           # Giao diện hóa đơn, lọc công nợ & thu tiền
        │   ├── PreviewHoaDonWindow.xaml  # Cửa sổ xem trước phiếu báo tiền trọ & xuất PDF
        │   └── SuCoView.xaml             # Giao diện tiếp nhận & xử lý sự cố bảo trì
        ├── App.xaml / App.xaml.cs        # Cấu hình Dependency Injection & Khởi động ứng dụng
        └── MainWindow.xaml               # Khung giao diện chính (Sidebar + Header + ContentControl)
```[cite: 17]

---

## 🗺️ Lộ trình phát triển (Hoàn thành 100%)

- [x] **Giai đoạn 1:** Khởi tạo cấu trúc dự án chuẩn MVVM, thiết lập Git và cài đặt các gói thư viện NuGet (`EF Core SQLite`, `CommunityToolkit.Mvvm`, `DependencyInjection`)[cite: 17].
- [x] **Giai đoạn 2:** Thiết kế toàn bộ thực thể cơ sở dữ liệu (`Entities.cs`), cấu hình `AppDbContext` và nạp dữ liệu mẫu tự động (`Seed Data`)[cite: 17].
- [x] **Giai đoạn 3:** Xây dựng dịch vụ xác thực (`AuthService`), màn hình đăng nhập (`LoginWindow`), khung giao diện chính (`MainWindow`) và bảng điều khiển tổng quan (`Dashboard`)[cite: 17].
- [x] **Giai đoạn 4:** Hoàn thiện bộ chức năng CRUD cho Quản lý Phòng, Quản lý Khách thuê và Quản lý Hợp đồng (Ký mới, Gia hạn, Thanh lý hoàn cọc)[cite: 17].
- [x] **Giai đoạn 5:** Xây dựng **Sơ đồ phòng trực quan (Core UI)** tự động đổi màu bằng `DataTrigger`, tích hợp bộ lọc thông minh và Panel Thao tác nhanh[cite: 17].
- [x] **Giai đoạn 6:** Xây dựng phân hệ Chốt chỉ số Điện Nước, Tự động tính & Lập hóa đơn hàng loạt, Thu tiền linh hoạt (đủ/một phần), Quản lý công nợ và Xem trước / Xuất hóa đơn **PDF**[cite: 17].
- [x] **Giai đoạn 7:** Xây dựng phân hệ Quản lý Sự cố & Bảo trì, nâng cấp Dashboard với Đa luồng (`async/await`), Biểu đồ đồ họa WPF, Xuất báo cáo **Excel (`.csv`)** và Sao lưu CSDL SQLite (`.db`)[cite: 17].

---

## ⚙️ Cài đặt & Chạy dự án

1. **Yêu cầu hệ thống:**
   - Hệ điều hành: Windows 10 / Windows 11[cite: 17].
   - Công cụ lập trình: **Visual Studio 2022 / 2026** (có cài đặt workload *.NET Desktop Development*)[cite: 17].
   - SDK: **.NET 10.0 SDK** (hoặc .NET 8.0+ tương thích)[cite: 17].

2. **Các bước khởi chạy:**
   ```bash
   # Bước 1: Clone kho lưu trữ về máy
   git clone <đường-dẫn-repo-của-bạn>
   cd QuanLyNhaTro

   # Bước 2: Mở file Solution bằng Visual Studio
   start src/QuanLyNhaTro.sln
   ```[cite: 17]
   - Trong Visual Studio, nhấn **`Ctrl + Shift + B`** để khôi phục các gói NuGet và biên dịch mã nguồn[cite: 17].
   - Nhấn **`F5`** để khởi chạy ứng dụng (File cơ sở dữ liệu `QuanLyNhaTro.db` sẽ tự động được khởi tạo ngay trong thư mục `bin/Debug/...` mà không cần cấu hình máy chủ SQL phức tạp)[cite: 17].