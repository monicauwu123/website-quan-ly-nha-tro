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
    public class LoaiPhongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhongController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private IQueryable<LoaiPhong> ApplyRoleFilter(IQueryable<LoaiPhong> query)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
            {
                query = query.Where(lp => lp.MaChuTro == userId);
            }

            // Người dùng được xem loại phòng để đọc thông tin phòng khi lựa chọn thuê.
            // Admin xem tất cả.
            return query;
        }

        // GET: api/LoaiPhong
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoaiPhong>>> GetLoaiPhong()
        {
            var data = await ApplyRoleFilter(_context.LoaiPhong.AsQueryable())
                .OrderBy(lp => lp.TenLoaiPhong)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/LoaiPhong/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LoaiPhong>> GetLoaiPhong(int id)
        {
            var loaiPhong = await ApplyRoleFilter(_context.LoaiPhong.AsQueryable())
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);

            if (loaiPhong == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy loại phòng"));

            return Ok(loaiPhong);
        }

        // POST: api/LoaiPhong
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<LoaiPhong>> PostLoaiPhong(LoaiPhong loaiPhong)
        {
            if (string.IsNullOrWhiteSpace(loaiPhong.TenLoaiPhong))
                return BadRequest(ApiResponse<object>.Loi("Tên loại phòng không được để trống"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                loaiPhong.MaChuTro = GetCurrentUserId();
            }

            _context.LoaiPhong.Add(loaiPhong);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLoaiPhong), new { id = loaiPhong.MaLoaiPhong }, loaiPhong);
        }

        // PUT: api/LoaiPhong/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutLoaiPhong(int id, LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
                return BadRequest(ApiResponse<object>.Loi("Mã loại phòng không khớp"));

            if (string.IsNullOrWhiteSpace(loaiPhong.TenLoaiPhong))
                return BadRequest(ApiResponse<object>.Loi("Tên loại phòng không được để trống"));

            var existing = await _context.LoaiPhong.FindAsync(id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy loại phòng"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && existing.MaChuTro != GetCurrentUserId())
                return Forbid();

            existing.TenLoaiPhong = loaiPhong.TenLoaiPhong;
            existing.MoTa = loaiPhong.MoTa;

            if (GetCurrentRole() == VaiTroConst.Admin)
                existing.MaChuTro = loaiPhong.MaChuTro;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/LoaiPhong/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteLoaiPhong(int id)
        {
            var loaiPhong = await _context.LoaiPhong.FindAsync(id);
            if (loaiPhong == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy loại phòng"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && loaiPhong.MaChuTro != GetCurrentUserId())
                return Forbid();

            var dangDuocDung = await _context.Phong.AnyAsync(p => p.MaLoaiPhong == id);
            if (dangDuocDung)
                return BadRequest(ApiResponse<object>.Loi("Không thể xóa loại phòng đang được sử dụng"));

            _context.LoaiPhong.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
