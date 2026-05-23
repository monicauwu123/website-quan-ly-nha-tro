using DoAnSE104.Data;
using DoAnSE104.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnSE104.Helpers
{
    public static class StartupDatabaseInitializer
    {
        public static void Initialize(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var configuration = services.GetRequiredService<IConfiguration>();

                // Cập nhật database và dữ liệu nền khi ứng dụng khởi động.
                EnsureDatabaseReady(context, configuration);
                EnsureDefaultRoomStatuses(context);
                EnsureDefaultAdmin(context);

                if (configuration.GetValue<bool>("Database:SeedSampleData"))
                {
                    InvokeSampleDataSeederIfAvailable(context);
                }

                EnsureStoredPasswordsAreHashed(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Lỗi khi tự động cập nhật database hoặc seed dữ liệu mặc định. Inner: {InnerMessage}", ex.InnerException?.Message);
            }
        }

        private static void EnsureDatabaseReady(ApplicationDbContext context, IConfiguration configuration)
        {
            // Chỉ xóa database khi cấu hình demo yêu cầu rõ ràng.
            if (configuration.GetValue<bool>("Database:RecreateOnStartup"))
            {
                context.Database.EnsureDeleted();
            }

            context.Database.EnsureCreated();

            if (context.Database.IsSqlServer())
            {
                context.EnsureCustomSchema();
            }
        }

        private static void EnsureDefaultRoomStatuses(ApplicationDbContext context)
        {
            if (context.TrangThai.Any()) return;

            context.TrangThai.AddRange(
                new TrangThai { TenTrangThai = "Còn trống" },
                new TrangThai { TenTrangThai = "Đã thuê" },
                new TrangThai { TenTrangThai = "Đang sửa chữa" },
                new TrangThai { TenTrangThai = "Ngưng hoạt động" }
            );

            context.SaveChanges();
        }

        private static void EnsureDefaultAdmin(ApplicationDbContext context)
        {
            if (context.Users.Any(u => u.TenDangNhap == "Admin")) return;

            context.Users.Add(new User
            {
                TenDangNhap = "Admin",
                HoTen = "Administrator",
                Email = "admin@example.com",
                SoDienThoai = "0123456789",
                VaiTro = VaiTroConst.Admin,
                MatKhau = BCrypt.Net.BCrypt.HashPassword("Admin123")
            });

            context.SaveChanges();
        }

        private static void InvokeSampleDataSeederIfAvailable(ApplicationDbContext context)
        {
            var seederType = typeof(ApplicationDbContext).Assembly.GetType("DoAnSE104.Data.SampleDataSeeder");
            var seedMethod = seederType?.GetMethod(
                "Seed",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ApplicationDbContext) },
                modifiers: null);

            seedMethod?.Invoke(null, new object[] { context });
        }

        private static void EnsureStoredPasswordsAreHashed(ApplicationDbContext context)
        {
            var usersWithPlainPassword = context.Users
                .Where(u => !string.IsNullOrWhiteSpace(u.MatKhau))
                .AsEnumerable()
                .Where(u => !IsBcryptHash(u.MatKhau))
                .ToList();

            if (usersWithPlainPassword.Count == 0) return;

            // Bảo vệ tài khoản cũ nếu dữ liệu demo còn lưu mật khẩu dạng plain text.
            foreach (var user in usersWithPlainPassword)
            {
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            }

            context.SaveChanges();
        }

        private static bool IsBcryptHash(string value)
        {
            return value.Length == 60
                && (value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$"));
        }
    }
}
