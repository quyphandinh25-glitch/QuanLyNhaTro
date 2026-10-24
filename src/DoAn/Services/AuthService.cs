using System.Linq;
using DoAn.Data;
using DoAn.Helpers;
using DoAn.Models;

namespace DoAn.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;

        // Lưu phiên đăng nhập hiện tại của Chủ trọ
        public static NguoiDung? CurrentUser { get; private set; }

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        public bool Login(string username, string password)
        {
            var user = _db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null) return false;

            string hashedInput = PasswordHelper.HashPassword(password);

            // Chấp nhận cả mật khẩu thường "123456" (từ Seed Data) hoặc mật khẩu đã băm SHA256
            if (user.MatKhauHash == password || user.MatKhauHash == hashedInput)
            {
                CurrentUser = user;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}