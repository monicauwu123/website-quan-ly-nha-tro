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
    public class NhaTroController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NhaTroController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        // GET: api/NhaTro
        [HttpGet]
        public async Task<IActionResult> GetNhaTro()
        {
            try
            {
                var role = GetCurrentRole();
                IQueryable<NhaTro> query = _context.NhaTro;

                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    query = query.Where(n => n.MaChuTro == userId);
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    // NguoiDung chỉ thấy nhà trọ của phòng mình đang thuê
                    var userId = GetCurrentUserId();
                    var maNhaTroList = await _context.NguoiThue
                        .Where(nt => nt.MaNguoiDung == userId)
                        .Join(_context.Phong, nt => nt.MaPhong, p => p.MaPhong, (nt, p) => p.MaNhaTro)
                        .Distinct()
                        .ToListAsync();
                    query = query.Where(n => maNhaTroList.Contains(n.MaNhaTro));
                }
                // Admin: không filter

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<NhaTro>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NhaTro/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNhaTro(int id)
        {
            try
            {
                var nhaTro = await _context.NhaTro.FindAsync(id);
                if (nhaTro == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro && nhaTro.MaChuTro != userId)
                    return Forbid();

                if (role == VaiTroConst.NguoiDung)
                {
                    var coQuyen = await _context.NguoiThue
                        .Where(nt => nt.MaNguoiDung == userId)
                        .Join(_context.Phong, nt => nt.MaPhong, p => p.MaPhong, (nt, p) => p.MaNhaTro)
                        .AnyAsync(maNhaTro => maNhaTro == id);
                    if (!coQuyen) return Forbid();
                }

                return Ok(ApiResponse<NhaTro>.Ok(nhaTro));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/NhaTro — Admin và ChuTro
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostNhaTro([FromBody] NhaTro nhaTro)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                // ChuTro tự động gán mình là chủ
                if (role == VaiTroConst.ChuTro)
                    nhaTro.MaChuTro = userId;

                _context.NhaTro.Add(nhaTro);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNhaTro), new { id = nhaTro.MaNhaTro },
                    ApiResponse<NhaTro>.Ok(nhaTro, "Tạo nhà trọ thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/NhaTro/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutNhaTro(int id, [FromBody] NhaTro nhaTro)
        {
            try
            {
                if (id != nhaTro.MaNhaTro)
                    return BadRequest(ApiResponse<object>.Loi("Mã nhà trọ không khớp"));

                var existing = await _context.NhaTro.FindAsync(id);
                if (existing == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro && existing.MaChuTro != GetCurrentUserId())
                    return Forbid();

                _context.Entry(nhaTro).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<NhaTro>.Ok(nhaTro, "Cập nhật thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/NhaTro/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteNhaTro(int id)
        {
            try
            {
                var nhaTro = await _context.NhaTro.FindAsync(id);
                if (nhaTro == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro && nhaTro.MaChuTro != GetCurrentUserId())
                    return Forbid();

                _context.NhaTro.Remove(nhaTro);
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
