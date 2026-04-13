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

        // GET: api/User
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
            }

            return user;
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
            }

            // Kiá»ƒm tra náº¿u lÃ  tÃ i khoáº£n admin thÃ¬ khÃ´ng cho phÃ©p thay Ä‘á»•i vai trÃ²
            if (user.VaiTro == "Admin" && userDto.VaiTro != "Admin")
            {
                return BadRequest(new { thongBao = "KhÃ´ng thá»ƒ thay Ä‘á»•i vai trÃ² cá»§a tÃ i khoáº£n admin" });
            }

            // Kiá»ƒm tra tÃªn Ä‘Äƒng nháº­p Ä‘Ã£ tá»“n táº¡i chÆ°a (náº¿u cÃ³ thay Ä‘á»•i)
            if (user.TenDangNhap != userDto.TenDangNhap)
            {
                if (await _context.Users.AnyAsync(u => u.TenDangNhap == userDto.TenDangNhap))
                {
                    return BadRequest(new { thongBao = "TÃªn Ä‘Äƒng nháº­p Ä‘Ã£ tá»“n táº¡i" });
                }
            }

            // Kiá»ƒm tra email Ä‘Ã£ tá»“n táº¡i chÆ°a (náº¿u cÃ³ thay Ä‘á»•i)
            if (user.Email != userDto.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                {
                    return BadRequest(new { thongBao = "Email Ä‘Ã£ tá»“n táº¡i" });
                }
            }

            // Cáº­p nháº­t thÃ´ng tin
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
                    return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { thongBao = "Cáº­p nháº­t thÃ´ng tin thÃ nh cÃ´ng" });
        }

        // DELETE: api/User/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
            }

            // Kiá»ƒm tra náº¿u lÃ  tÃ i khoáº£n admin thÃ¬ khÃ´ng cho phÃ©p xÃ³a
            if (user.VaiTro == "Admin")
            {
                return BadRequest(new { thongBao = "KhÃ´ng thá»ƒ xÃ³a tÃ i khoáº£n admin" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { thongBao = "XÃ³a ngÆ°á»i dÃ¹ng thÃ nh cÃ´ng" });
        }

        // GET: api/User/profile
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
            }

            return user;
        }

        // PUT: api/User/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UserProfileDto profileDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
            }

            // Kiá»ƒm tra email Ä‘Ã£ tá»“n táº¡i chÆ°a (náº¿u cÃ³ thay Ä‘á»•i)
            if (user.Email != profileDto.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == profileDto.Email))
                {
                    return BadRequest(new { thongBao = "Email Ä‘Ã£ tá»“n táº¡i" });
                }
            }

            // Cáº­p nháº­t thÃ´ng tin
            user.HoTen = profileDto.HoTen;
            user.Email = profileDto.Email;
            user.SoDienThoai = profileDto.SoDienThoai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userId))
                {
                    return NotFound(new { thongBao = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { thongBao = "Cáº­p nháº­t thÃ´ng tin thÃ nh cÃ´ng" });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.MaNguoiDung == id);
        }
    }
} 
