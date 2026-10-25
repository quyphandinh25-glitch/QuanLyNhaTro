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
        private string _tieuDeManHinh = "Tổng quan hệ thống";

        public string TenNguoiDung => AuthService.CurrentUser?.HoTen ?? "Chủ Trọ Quản Lý";
        public string TenNhaTro => AuthService.CurrentUser?.TenNhaTro ?? "Nhà Trọ An Cư";

        public MainViewModel()
        {
            // Mặc định khi mở lên sẽ vào màn hình Dashboard
            ChuyenManHinh("Dashboard");
        }

        [RelayCommand]
        private void ChuyenManHinh(string? tenManHinh)
        {
            switch (tenManHinh)
            {
                case "Dashboard":
                    TieuDeManHinh = "📊 Tổng quan hệ thống";
                    CurrentView = App.ServiceProvider.GetRequiredService<DashboardViewModel>();
                    break;
                case "SoDoPhong":
                    TieuDeManHinh = "🗺️ Sơ đồ phòng trực quan";
                    CurrentView = App.ServiceProvider.GetRequiredService<SoDoPhongViewModel>();
                    break;
                case "Phong":
                    TieuDeManHinh = "🚪 Quản lý Danh mục Phòng & Loại phòng";
                    CurrentView = App.ServiceProvider.GetRequiredService<PhongViewModel>();
                    break;
                case "KhachThue":
                    TieuDeManHinh = "👥 Quản lý Hồ sơ Khách thuê";
                    CurrentView = App.ServiceProvider.GetRequiredService<KhachThueViewModel>();
                    break;
                case "HopDong":
                    TieuDeManHinh = "📄 Quản lý Hợp đồng thuê phòng";
                    CurrentView = App.ServiceProvider.GetRequiredService<HopDongViewModel>();
                    break;
                case "DienNuoc":
                    TieuDeManHinh = "⚡ Chốt chỉ số Điện & Nước hàng tháng";
                    CurrentView = App.ServiceProvider.GetRequiredService<DienNuocViewModel>();
                    break;
                case "HoaDon":
                    TieuDeManHinh = "🧾 Quản lý Hóa đơn & Tính tiền trọ";
                    CurrentView = App.ServiceProvider.GetRequiredService<HoaDonViewModel>();
                    break;
                case "SuCo":
                    TieuDeManHinh = "🛠️ Quản lý Sự cố & Bảo trì phòng";
                    CurrentView = App.ServiceProvider.GetRequiredService<SuCoViewModel>();
                    break;
            }
        }
    }
}