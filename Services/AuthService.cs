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

            if (dangKyDto.VaiTro == VaiTroConst.NguoiDung)
            {
                if (string.IsNullOrWhiteSpace(dangKyDto.HoTen))
                    throw new Exception("Họ tên không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.SoDienThoai))
                    throw new Exception("Số điện thoại không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.CCCD))
                    throw new Exception("CCCD/CMND không được để trống");

                if (dangKyDto.NgaySinh == null)
                    throw new Exception("Ngày sinh không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.GioiTinh))
                    throw new Exception("Giới tính không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.QuocTich))
                    throw new Exception("Quốc tịch không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.DiaChi))
                    throw new Exception("Địa chỉ không được để trống");

                if (string.IsNullOrWhiteSpace(dangKyDto.AnhCccdMatTruoc))
                    throw new Exception("Vui lòng upload ảnh CCCD mặt trước");

                if (string.IsNullOrWhiteSpace(dangKyDto.AnhCccdMatSau))
                    throw new Exception("Vui lòng upload ảnh CCCD mặt sau");
            }

            var nguoiDung = new User
            {
                TenDangNhap = dangKyDto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dangKyDto.MatKhau),
                Email = dangKyDto.Email,
                HoTen = dangKyDto.HoTen ?? string.Empty,
                SoDienThoai = dangKyDto.SoDienThoai ?? string.Empty,
                CCCD = dangKyDto.CCCD,
                NgaySinh = dangKyDto.NgaySinh,
                GioiTinh = dangKyDto.GioiTinh,
                QuocTich = dangKyDto.QuocTich ?? (dangKyDto.VaiTro == VaiTroConst.NguoiDung ? "Việt Nam" : null),
                DiaChi = dangKyDto.DiaChi,
                NoiCongTac = dangKyDto.NoiCongTac,
                AnhCccdMatTruoc = dangKyDto.AnhCccdMatTruoc,
                AnhCccdMatSau = dangKyDto.AnhCccdMatSau,
                VaiTro = dangKyDto.VaiTro
            };

            // Lưu thông tin ngân hàng nếu là ChuTro
            if (dangKyDto.VaiTro == VaiTroConst.ChuTro)
            {
                nguoiDung.TenNganHang = string.IsNullOrWhiteSpace(dangKyDto.TenNganHang) ? null : dangKyDto.TenNganHang.Trim();
                nguoiDung.MaNganHang  = string.IsNullOrWhiteSpace(dangKyDto.MaNganHang)  ? null : dangKyDto.MaNganHang.Trim();
                nguoiDung.SoTaiKhoan  = string.IsNullOrWhiteSpace(dangKyDto.SoTaiKhoan)  ? null : dangKyDto.SoTaiKhoan.Trim();
                nguoiDung.TenChuTaiKhoan = string.IsNullOrWhiteSpace(dangKyDto.TenChuTaiKhoan) ? null : dangKyDto.TenChuTaiKhoan.Trim();
                nguoiDung.NoiDungChuyenKhoanMacDinh = string.IsNullOrWhiteSpace(dangKyDto.NoiDungChuyenKhoanMacDinh) ? null : dangKyDto.NoiDungChuyenKhoanMacDinh.Trim();
            }

            _context.Users.Add(nguoiDung);
            await _context.SaveChangesAsync();

            var token = TaoJwtToken(nguoiDung);

            return MapToDto(nguoiDung, token);
        }

        public async Task<NguoiDungResponseDto> DangNhap(DangNhapDto dangNhapDto)
        {
            // Nếu client gửi vai trò thì kiểm tra giá trị hợp lệ.
            if (!string.IsNullOrWhiteSpace(dangNhapDto.VaiTro) && !_vaiTroHopLe.Contains(dangNhapDto.VaiTro))
                throw new Exception($"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", _vaiTroHopLe)}");

            var loginIdentifier = dangNhapDto.TenDangNhap.Trim().ToLower();
            var nguoiDung = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.TenDangNhap.ToLower() == loginIdentifier ||
                    u.Email.ToLower() == loginIdentifier);

            if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(dangNhapDto.MatKhau, nguoiDung.MatKhau))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không đúng");

            if (!nguoiDung.TrangThai)
                throw new Exception("Tài khoản đã bị khóa");

            // Nếu có chọn vai trò, tài khoản phải khớp vai trò đó.
            if (!string.IsNullOrWhiteSpace(dangNhapDto.VaiTro) && nguoiDung.VaiTro != dangNhapDto.VaiTro)
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
            CCCD = u.CCCD,
            NgaySinh = u.NgaySinh,
            GioiTinh = u.GioiTinh,
            QuocTich = u.QuocTich,
            DiaChi = u.DiaChi,
            NoiCongTac = u.NoiCongTac,
            AnhCccdMatTruoc = u.AnhCccdMatTruoc,
            AnhCccdMatSau = u.AnhCccdMatSau,
            VaiTro = u.VaiTro,
            Token = token
        };
    }
}
