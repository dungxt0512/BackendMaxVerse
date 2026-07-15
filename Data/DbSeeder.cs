using MaxVerse.API.Models;

namespace MaxVerse.API.Data;

/// <summary>
/// Seeder tạo tài khoản Admin mặc định khi khởi động ứng dụng (nếu chưa có).
/// Lý do tách riêng: mật khẩu Admin cần hash bằng BCrypt, không thể insert
/// trực tiếp bằng SQL script như dữ liệu sản phẩm.
/// </summary>
public static class DbSeeder
{
    public static void SeedAdmin(MaxVerseDbContext context)
    {
        var adminExists = context.Users.Any(u => u.Email == "admin@maxverse.com");
        if (adminExists) return;

        var admin = new User
        {
            FullName = "Quản trị viên MaxVerse",
            Email = "admin@maxverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            RoleId = 1, // Admin
            IsActive = true
        };

        context.Users.Add(admin);
        context.SaveChanges();
    }
}
