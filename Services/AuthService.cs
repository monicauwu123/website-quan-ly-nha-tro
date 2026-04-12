using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;

namespace DoAnSE104.Services
{
    public interface IAuthService
    {
        Task<NguoiDungResponseDto> DangKy(DangKyDto dangKyDto);
        Task<NguoiDungResponseDto> DangNhap(DangNhapDto dangNhapDto);
        string TaoJwtToken(User nguoiDung);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> _vaiTroHopLe = new()
        {
            VaiTroConst.Admin,
            VaiTroConst.ChuTro,
            VaiTroConst.NguoiDung
        };

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<NguoiDungResponseDto> DangKy(DangKyDto dangKyDto)
        {
            // Validate vai trò
            if (!_vaiTroHopLe.Contains(dangKyDto.VaiTro))
                throw new Exception($"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", _vaiTroHopLe)}");

            // Không cho phép tự đăng ký Admin
            if (dangKyDto.VaiTro == VaiTroConst.Admin)
                throw new Exception("Không thể tự đăng ký tài khoản Admin");

            if (await _context.Users.AnyAsync(u => u.TenDangNhap == dangKyDto.TenDangNhap))
                throw new Exception("Tên đăng nhập đã tồn tại");

            if (await _context.Users.AnyAsync(u => u.Email == dangKyDto.Email))
                throw new Exception("Email đã tồn tại");

            var nguoiDung = new User
            {
                TenDangNhap = dangKyDto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dangKyDto.MatKhau),
                Email = dangKyDto.Email,
                HoTen = dangKyDto.HoTen ?? string.Empty,
                SoDienThoai = dangKyDto.SoDienThoai ?? string.Empty,
                VaiTro = dangKyDto.VaiTro
            };

            _context.Users.Add(nguoiDung);
            await _context.SaveChangesAsync();

            var token = TaoJwtToken(nguoiDung);

            return MapToDto(nguoiDung, token);
        }

        public async Task<NguoiDungResponseDto> DangNhap(DangNhapDto dangNhapDto)
        {
            // Validate vai trò được chọn
            if (!_vaiTroHopLe.Contains(dangNhapDto.VaiTro))
                throw new Exception($"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", _vaiTroHopLe)}");

            var nguoiDung = await _context.Users
                .FirstOrDefaultAsync(u => u.TenDangNhap == dangNhapDto.TenDangNhap);

            if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(dangNhapDto.MatKhau, nguoiDung.MatKhau))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không đúng");

            if (!nguoiDung.TrangThai)
                throw new Exception("Tài khoản đã bị khóa");

            // Kiểm tra vai trò có khớp không
            if (nguoiDung.VaiTro != dangNhapDto.VaiTro)
                throw new Exception($"Tài khoản này không có vai trò '{dangNhapDto.VaiTro}'");

            var token = TaoJwtToken(nguoiDung);

            return MapToDto(nguoiDung, token);
        }

        public string TaoJwtToken(User nguoiDung)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, nguoiDung.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, nguoiDung.TenDangNhap),
                new Claim(ClaimTypes.Email, nguoiDung.Email),
                new Claim(ClaimTypes.Role, nguoiDung.VaiTro),
                // Custom claims để dễ truy xuất
                new Claim("MaNguoiDung", nguoiDung.MaNguoiDung.ToString()),
                new Claim("VaiTro", nguoiDung.VaiTro)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static NguoiDungResponseDto MapToDto(User u, string token) => new()
        {
            MaNguoiDung = u.MaNguoiDung,
            TenDangNhap = u.TenDangNhap,
            Email = u.Email,
            HoTen = u.HoTen,
            SoDienThoai = u.SoDienThoai,
            VaiTro = u.VaiTro,
            Token = token
        };
    }
}
