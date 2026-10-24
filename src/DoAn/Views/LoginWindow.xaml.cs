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
                lblError.Text = "⚠️ Sai tên đăng nhập hoặc mật khẩu! (Mặc định: admin / 123456)";
                lblError.Visibility = Visibility.Visible;
            }
        }
    }
}