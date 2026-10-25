using System;
using System.IO;
using System.Windows;
using DoAn.Data;
using DoAn.Services;
using DoAn.ViewModels;
using DoAn.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DoAn
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Database Context
            services.AddDbContext<AppDbContext>();

            // 2. Services
            services.AddSingleton<AuthService>();

            // 3. ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<PhongViewModel>();
            services.AddTransient<KhachThueViewModel>();
            services.AddTransient<HopDongViewModel>();
            // 4. Views / Windows
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<SoDoPhongViewModel>();
            services.AddTransient<DienNuocViewModel>();
            services.AddTransient<HoaDonViewModel>();
            services.AddTransient<SuCoViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.InitializeAndSeed();
                }

                // Khởi chạy màn hình Đăng nhập đầu tiên
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                File.AppendAllText("app_error.log", $"[{DateTime.Now}] Lỗi khởi động: {ex}\n");
                MessageBox.Show($"Lỗi khởi tạo hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}