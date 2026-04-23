using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NguoiThueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NguoiThueController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<bool> PhongThuocChuTro(int maPhong, int maChuTro)
        {
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == maChuTro);
        }

        private async Task<string?> ValidateNguoiThue(NguoiThue nguoiThue)
        {
            if (string.IsNullOrWhiteSpace(nguoiThue.HoTen))
                return "Họ tên người thuê không được để trống";

            if (!await _context.Phong.AnyAsync(p => p.MaPhong == nguoiThue.MaPhong))
                return "Phòng không tồn tại";

            if (nguoiThue.MaNguoiDung.HasValue && !await _context.Users.AnyAsync(u => u.MaNguoiDung == nguoiThue.MaNguoiDung.Value))
                return "Tài khoản người dùng liên kết không tồn tại";

            return null;
        }

        // GET: api/NguoiThue
        [HttpGet]
        public async Task<IActionResult> GetNguoiThue()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<NguoiThue> query = _context.NguoiThue;

                if (role == VaiTroConst.ChuTro)
                {
                    // ChuTro chỉ thấy người thuê ở nhà trọ của mình
                    var maNhaTroList = await _context.NhaTro
                        .Where(n => n.MaChuTro == userId)
                        .Select(n => n.MaNhaTro)
                        .ToListAsync();

                    var maPhongList = await _context.Phong
                        .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                        .Select(p => p.MaPhong)
                        .ToListAsync();

                    query = query.Where(nt => maPhongList.Contains(nt.MaPhong));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    // NguoiDung chỉ thấy profile mình
                    query = query.Where(nt => nt.MaNguoiDung == userId);
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<NguoiThue>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NguoiThue/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue.FindAsync(id);
                if (nguoiThue == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.NguoiDung && nguoiThue.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro)
                {
                    var maNhaTroList = await _context.NhaTro
                        .Where(n => n.MaChuTro == userId)
                        .Select(n => n.MaNhaTro)
                        .ToListAsync();
                    var maPhongList = await _context.Phong
                        .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                        .Select(p => p.MaPhong)
                        .ToListAsync();
                    if (!maPhongList.Contains(nguoiThue.MaPhong))
                        return Forbid();
                }

                return Ok(ApiResponse<NguoiThue>.Ok(nguoiThue));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NguoiThue/Search
        [HttpGet("Search")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> SearchNguoiThue(string? keyword)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<NguoiThue> query = _context.NguoiThue;

                if (role == VaiTroConst.ChuTro)
                {
                    var maNhaTroList = await _context.NhaTro
                        .Where(n => n.MaChuTro == userId)
                        .Select(n => n.MaNhaTro)
                        .ToListAsync();
                    var maPhongList = await _context.Phong
                        .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                        .Select(p => p.MaPhong)
                        .ToListAsync();
                    query = query.Where(nt => maPhongList.Contains(nt.MaPhong));
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(n =>
                        n.HoTen.Contains(keyword) ||
                        (n.CCCD != null && n.CCCD.Contains(keyword)) ||
                        (n.SDT != null && n.SDT.Contains(keyword)) ||
                        (n.Email != null && n.Email.Contains(keyword)));
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<NguoiThue>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/NguoiThue
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostNguoiThue([FromBody] NguoiThue nguoiThue)
        {
            try
            {
                var loiValidation = await ValidateNguoiThue(nguoiThue);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro && !await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                    return Forbid();

                if (!nguoiThue.MaNguoiDung.HasValue)
                {
                    var linkedUser = await _context.Users.FirstOrDefaultAsync(u =>
                        (!string.IsNullOrWhiteSpace(nguoiThue.Email) && u.Email == nguoiThue.Email) ||
                        (!string.IsNullOrWhiteSpace(nguoiThue.SDT) && u.SoDienThoai == nguoiThue.SDT));

                    if (linkedUser != null)
                        nguoiThue.MaNguoiDung = linkedUser.MaNguoiDung;
                }

                _context.NguoiThue.Add(nguoiThue);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNguoiThue), new { id = nguoiThue.MaNguoiThue },
                    ApiResponse<NguoiThue>.Ok(nguoiThue, "Thêm người thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/NguoiThue/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutNguoiThue(int id, [FromBody] NguoiThue nguoiThue)
        {
            try
            {
                if (id != nguoiThue.MaNguoiThue)
                    return BadRequest(ApiResponse<object>.Loi("Mã người thuê không khớp"));

                var existing = await _context.NguoiThue.AsNoTracking().FirstOrDefaultAsync(nt => nt.MaNguoiThue == id);
                if (existing == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var loiValidation = await ValidateNguoiThue(nguoiThue);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro)
                {
                    if (!await PhongThuocChuTro(existing.MaPhong, userId) || !await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                        return Forbid();
                }

                _context.Entry(nguoiThue).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<NguoiThue>.Ok(nguoiThue, "Cập nhật thành công"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.NguoiThue.Any(e => e.MaNguoiThue == id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/NguoiThue/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue.FindAsync(id);
                if (nguoiThue == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();
                if (role == VaiTroConst.ChuTro && !await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                    return Forbid();

                _context.NguoiThue.Remove(nguoiThue);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null!, "Xóa thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
