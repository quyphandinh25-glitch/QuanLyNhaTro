using System.Windows;
using DoAn.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DoAn.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            txtUsername.Focus(); // Tự động đặt con trỏ chuột vào ô Tài khoản khi mở app
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (_authService.Login(user, pass))
            {
                // Mở MainWindow từ DI Container
                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                txtError.Text = "⚠️ Tên đăng nhập hoặc mật khẩu không chính xác!";
                txtError.Visibility = Visibility.Visible;
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}