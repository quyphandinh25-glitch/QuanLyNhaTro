using System.Windows;
using DoAn.Services;
using DoAn.ViewModels;
using DoAn.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DoAn
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var auth = App.ServiceProvider.GetRequiredService<AuthService>();
            auth.Logout();

            var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            this.Close();
        }
    }
}