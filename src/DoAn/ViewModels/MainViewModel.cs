using System;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoAn.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DoAn.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Chứa màn hình con đang hiển thị bên phải Sidebar
        [ObservableProperty]
        private object? _currentView;

        // Tiêu đề hiển thị trên thanh Header
        [ObservableProperty]
        private string _tieuDeManHinh = "📊 Tổng quan hệ thống";

        // Khai báo tường minh MoTaManHinh để không bao giờ bị báo đỏ
        private string _moTaManHinh = "Theo dõi nhanh công suất phòng, doanh thu và các cảnh báo quan trọng";
        public string MoTaManHinh
        {
            get => _moTaManHinh;
            set => SetProperty(ref _moTaManHinh, value);
        }

        public string TenNguoiDung => AuthService.CurrentUser?.HoTen ?? "Chủ Trọ Quản Lý";
        public string TenNhaTro => AuthService.CurrentUser?.TenNhaTro ?? "Nhà Trọ An Cư";
        public string NgayHienTai => $"📅 Hôm nay, {DateTime.Now:dd/MM/yyyy}";

        public MainViewModel()
        {
            // Mặc định khi mở lên sẽ vào màn hình Dashboard
            ChuyenManHinh("Dashboard");
        }

        
        private string? _manHinhHienTai;

        [RelayCommand]
        private void ChuyenManHinh(string? tenManHinh)
        {
            // Chặn nạp lại nếu đang ở đúng màn hình đó rồi
            if (string.IsNullOrEmpty(tenManHinh) || _manHinhHienTai == tenManHinh)
                return;

            _manHinhHienTai = tenManHinh;

            switch (tenManHinh)
            {
                case "Dashboard":
                    TieuDeManHinh = "📊 Tổng quan hệ thống";
                    MoTaManHinh = "Theo dõi nhanh công suất phòng, doanh thu và các cảnh báo quan trọng";
                    CurrentView = App.ServiceProvider.GetRequiredService<DashboardViewModel>();
                    break;
                case "SoDoPhong":
                    TieuDeManHinh = "🗺️ Sơ đồ phòng trực quan";
                    MoTaManHinh = "Quan sát trạng thái thực tế từng phòng theo tầng và thao tác nhanh";
                    CurrentView = App.ServiceProvider.GetRequiredService<SoDoPhongViewModel>();
                    break;
                case "Phong":
                    TieuDeManHinh = "🚪 Quản lý Danh mục Phòng & Loại phòng";
                    MoTaManHinh = "Thêm mới, cập nhật giá thuê và thông tin chi tiết các phòng";
                    CurrentView = App.ServiceProvider.GetRequiredService<PhongViewModel>();
                    break;
                case "KhachThue":
                    TieuDeManHinh = "👥 Quản lý Hồ sơ Khách thuê";
                    MoTaManHinh = "Lưu trữ thông tin CCCD, số điện thoại và tình trạng tạm trú";
                    CurrentView = App.ServiceProvider.GetRequiredService<KhachThueViewModel>();
                    break;
                case "HopDong":
                    TieuDeManHinh = "📄 Quản lý Hợp đồng thuê phòng";
                    MoTaManHinh = "Lập hợp đồng mới, gia hạn kỳ hạn thuê hoặc làm thủ tục trả phòng & hoàn cọc";
                    CurrentView = App.ServiceProvider.GetRequiredService<HopDongViewModel>();
                    break;
                case "DienNuoc":
                    TieuDeManHinh = "⚡ Chốt chỉ số Điện & Nước hàng tháng";
                    MoTaManHinh = "Ghi nhận chỉ số công tơ cũ - mới cho các phòng đang có hợp đồng lưu trú";
                    CurrentView = App.ServiceProvider.GetRequiredService<DienNuocViewModel>();
                    break;
                case "HoaDon":
                    TieuDeManHinh = "🧾 Quản lý Hóa đơn & Tính tiền trọ";
                    MoTaManHinh = "Tính tiền phòng tự động, theo dõi công nợ và in phiếu báo tiền trọ";
                    CurrentView = App.ServiceProvider.GetRequiredService<HoaDonViewModel>();
                    break;
                case "SuCo":
                    TieuDeManHinh = "🛠️ Quản lý Sự cố & Bảo trì phòng";
                    MoTaManHinh = "Tiếp nhận phản ánh hư hỏng từ khách thuê và theo dõi tiến độ sửa chữa";
                    CurrentView = App.ServiceProvider.GetRequiredService<SuCoViewModel>();
                    break;
            }
        }

        [RelayCommand]
        private void DangXuat()
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?", "Xác nhận đăng xuất",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var loginWinType = Application.Current.GetType().Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == "LoginWindow" || t.Name == "DangNhapWindow");

                if (loginWinType != null)
                {
                    var mainWin = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (ActivatorUtilities.CreateInstance(App.ServiceProvider, loginWinType) is Window loginWin)
                    {
                        Application.Current.MainWindow = loginWin;
                        loginWin.Show();
                        mainWin?.Close();
                    }
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}