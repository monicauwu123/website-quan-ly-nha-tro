using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            // Đọc user id từ JWT, không parse khi token thiếu claim.
            var rawUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { thongBao = "Không tìm thấy người dùng" });
            }

            return user;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { thongBao = "Không tìm thấy người dùng" });
            }

            // Không cho đổi vai trò của tài khoản admin.
            if (user.VaiTro == "Admin" && userDto.VaiTro != "Admin")
            {
                return BadRequest(new { thongBao = "Không thể thay đổi vai trò của tài khoản admin" });
            }

            // Nếu đổi tên đăng nhập thì kiểm tra trùng trước.
            if (user.TenDangNhap != userDto.TenDangNhap)
            {
                if (await _context.Users.AnyAsync(u => u.TenDangNhap == userDto.TenDangNhap))
                {
                    return BadRequest(new { thongBao = "Tên đăng nhập đã tồn tại" });
                }
            }

            // Nếu đổi email thì kiểm tra trùng trước.
            if (user.Email != userDto.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                {
                    return BadRequest(new { thongBao = "Email đã tồn tại" });
                }
            }

            // Ghi các thông tin được phép sửa.
            user.HoTen = userDto.HoTen;
            user.TenDangNhap = userDto.TenDangNhap;
            user.Email = userDto.Email;
            user.SoDienThoai = userDto.SoDienThoai;
            user.VaiTro = userDto.VaiTro;
            user.TrangThai = userDto.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound(new { thongBao = "Không tìm thấy người dùng" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { thongBao = "Cập nhật thông tin thành công" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { thongBao = "Không tìm thấy người dùng" });
            }

            // Không cho xóa tài khoản admin.
            if (user.VaiTro == "Admin")
            {
                return BadRequest(new { thongBao = "Không thể xóa tài khoản admin" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { thongBao = "Xóa người dùng thành công" });
        }

        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { thongBao = "Token không hợp lệ" });
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { thongBao = "Không tìm thấy người dùng" });
            }

            return user;
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UserProfileDto profileDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { thongBao = "Token không hợp lệ" });
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { thongBao = "Không tìm thấy người dùng" });
            }

            // Nếu đổi email thì kiểm tra trùng trước.
            if (user.Email != profileDto.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == profileDto.Email))
                {
                    return BadRequest(new { thongBao = "Email đã tồn tại" });
                }
            }

            // Ghi các thông tin hồ sơ được phép sửa.
            user.HoTen = profileDto.HoTen;
            user.Email = profileDto.Email;
            user.SoDienThoai = profileDto.SoDienThoai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userId.Value))
                {
                    return NotFound(new { thongBao = "Không tìm thấy người dùng" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { thongBao = "Cập nhật thông tin thành công" });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.MaNguoiDung == id);
        }
    }
} 
