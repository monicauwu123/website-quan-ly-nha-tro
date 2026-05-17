using System.Security.Cryptography;
using BCrypt.Net;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DoAnSE104.Services
{
    public interface IAccountService
    {
        Task<ThongTinTaiKhoanDto> LayThongTin(int maNguoiDung);
        Task<ThongTinTaiKhoanDto> CapNhatThongTin(int maNguoiDung, CapNhatThongTinDto dto);
        Task DoiMatKhau(int maNguoiDung, DoiMatKhauDto dto);
        Task QuenMatKhau(string email, string baseUrl);
        Task ResetMatKhau(ResetMatKhauDto dto);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ─── Lấy thông tin tài khoản ─────────────────────────────────────────

        public async Task<ThongTinTaiKhoanDto> LayThongTin(int maNguoiDung)
        {
            var user = await _context.Users.FindAsync(maNguoiDung)
                ?? throw new Exception("Không tìm thấy tài khoản");

            return MapToThongTinDto(user);
        }

        // ─── Cập nhật thông tin tài khoản ───────────────────────────────────

        public async Task<ThongTinTaiKhoanDto> CapNhatThongTin(int maNguoiDung, CapNhatThongTinDto dto)
        {
            var user = await _context.Users.FindAsync(maNguoiDung)
                ?? throw new Exception("Không tìm thấy tài khoản");

            // Kiểm tra email trùng với người khác
            if (user.Email != dto.Email)
            {
                bool emailTonTai = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.MaNguoiDung != maNguoiDung);
                if (emailTonTai)
                    throw new Exception("Email này đã được sử dụng bởi tài khoản khác");
            }

            user.HoTen = dto.HoTen.Trim();
            user.Email = dto.Email.Trim();
            user.SoDienThoai = dto.SoDienThoai ?? string.Empty;
            user.CCCD = dto.CCCD;
            user.NgaySinh = dto.NgaySinh;
            user.GioiTinh = dto.GioiTinh;
            user.QuocTich = dto.QuocTich;
            user.DiaChi = dto.DiaChi;
            user.NoiCongTac = dto.NoiCongTac;
            user.AnhCccdMatTruoc = dto.AnhCccdMatTruoc;
            user.AnhCccdMatSau = dto.AnhCccdMatSau;

            if (user.VaiTro == "ChuTro" || user.VaiTro == "Admin")
            {
                user.TenNganHang = string.IsNullOrWhiteSpace(dto.TenNganHang) ? null : dto.TenNganHang.Trim();
                user.MaNganHang = string.IsNullOrWhiteSpace(dto.MaNganHang) ? null : dto.MaNganHang.Trim();
                user.SoTaiKhoan = string.IsNullOrWhiteSpace(dto.SoTaiKhoan) ? null : dto.SoTaiKhoan.Trim();
                user.TenChuTaiKhoan = string.IsNullOrWhiteSpace(dto.TenChuTaiKhoan) ? null : dto.TenChuTaiKhoan.Trim();
                user.NoiDungChuyenKhoanMacDinh = string.IsNullOrWhiteSpace(dto.NoiDungChuyenKhoanMacDinh) ? null : dto.NoiDungChuyenKhoanMacDinh.Trim();
            }

            var danhSachNguoiThue = await _context.NguoiThue
                .Where(nt => nt.MaNguoiDung == maNguoiDung)
                .ToListAsync();

            foreach (var nt in danhSachNguoiThue)
            {
                nt.HoTen = user.HoTen;
                nt.CCCD = user.CCCD;
                nt.SDT = user.SoDienThoai;
                nt.Email = user.Email;
                nt.NgaySinh = user.NgaySinh;
                nt.GioiTinh = user.GioiTinh;
                nt.QuocTich = user.QuocTich;
                nt.DiaChi = user.DiaChi;
                nt.NoiCongTac = user.NoiCongTac;
                nt.AnhCccdMatTruoc = user.AnhCccdMatTruoc;
                nt.AnhCccdMatSau = user.AnhCccdMatSau;
            }

            await _context.SaveChangesAsync();

            return MapToThongTinDto(user);
        }

        // ─── Đổi mật khẩu ────────────────────────────────────────────────────

        public async Task DoiMatKhau(int maNguoiDung, DoiMatKhauDto dto)
        {
            var user = await _context.Users.FindAsync(maNguoiDung)
                ?? throw new Exception("Không tìm thấy tài khoản");

            if (!BCrypt.Net.BCrypt.Verify(dto.MatKhauCu, user.MatKhau))
                throw new Exception("Mật khẩu cũ không đúng");

            if (dto.MatKhauMoi != dto.NhapLaiMatKhau)
                throw new Exception("Mật khẩu nhập lại không khớp");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);

            await _context.SaveChangesAsync();
        }

        // ─── Quên mật khẩu — sinh OTP 6 số, gửi email ───────────────────────

        public async Task QuenMatKhau(string email, string baseUrl)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // Không tiết lộ email có tồn tại hay không (bảo mật)
            if (user == null)
                throw new Exception("Email không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");

            if (!user.TrangThai)
                throw new Exception("Tài khoản đã bị khóa, không thể đặt lại mật khẩu");

            // Sinh OTP 6 số
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(otp);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            // URL có email & raw otp để client tự điền vào form reset
            var resetUrl = $"{baseUrl}/reset-mat-khau?email={Uri.EscapeDataString(user.Email)}&token={otp}";

            await _emailService.GuiEmailResetMatKhau(
                toEmail: user.Email,
                hoTen: user.HoTen ?? user.TenDangNhap,
                token: otp,
                resetUrl: resetUrl
            );
        }

        // ─── Reset mật khẩu bằng OTP ─────────────────────────────────────────

        public async Task ResetMatKhau(ResetMatKhauDto dto)
        {
            if (dto.MatKhauMoi != dto.NhapLaiMatKhau)
                throw new Exception("Mật khẩu nhập lại không khớp");

            var normalizedEmail = (dto.Email ?? string.Empty).Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail)
                ?? throw new Exception("Email không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");

            if (string.IsNullOrEmpty(user.PasswordResetToken))
                throw new Exception("Chưa có yêu cầu đặt lại mật khẩu cho tài khoản này");

            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                throw new Exception("Mã OTP đã hết hạn. Vui lòng yêu cầu lại");

            if (!BCrypt.Net.BCrypt.Verify(dto.Token, user.PasswordResetToken))
                throw new Exception("Mã OTP không hợp lệ");

            // Đặt mật khẩu mới
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static ThongTinTaiKhoanDto MapToThongTinDto(User u) => new()
        {
            MaNguoiDung = u.MaNguoiDung,
            TenDangNhap = u.TenDangNhap,
            HoTen = u.HoTen,
            Email = u.Email,
            SoDienThoai = u.SoDienThoai,
            CCCD = u.CCCD,
            NgaySinh = u.NgaySinh,
            GioiTinh = u.GioiTinh,
            QuocTich = u.QuocTich,
            DiaChi = u.DiaChi,
            NoiCongTac = u.NoiCongTac,
            AnhCccdMatTruoc = u.AnhCccdMatTruoc,
            AnhCccdMatSau = u.AnhCccdMatSau,
            TenNganHang = u.TenNganHang,
            MaNganHang = u.MaNganHang,
            SoTaiKhoan = u.SoTaiKhoan,
            TenChuTaiKhoan = u.TenChuTaiKhoan,
            NoiDungChuyenKhoanMacDinh = u.NoiDungChuyenKhoanMacDinh,
            VaiTro = u.VaiTro,
            NgayTao = u.NgayTao,
            TrangThai = u.TrangThai
        };
    }
}
