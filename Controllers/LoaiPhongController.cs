using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoaiPhongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDeleteValidationService _deleteValidationService;

        public LoaiPhongController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _deleteValidationService = deleteValidationService;
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
                query = query.Where(lp =>
                    (lp.MaNhaTro != null && lp.NhaTro!.MaChuTro == userId) ||
                    (lp.MaNhaTro == null && lp.MaChuTro == userId));
            }

            return query;
        }

        private async Task<bool> ChuTroCoQuyenNhaTro(int? maNhaTro)
        {
            if (maNhaTro == null || maNhaTro <= 0)
                return false;

            var role = GetCurrentRole();
            if (role == VaiTroConst.Admin)
                return await _context.NhaTro.AnyAsync(n => n.MaNhaTro == maNhaTro.Value);

            var userId = GetCurrentUserId();
            return await _context.NhaTro.AnyAsync(n => n.MaNhaTro == maNhaTro.Value && n.MaChuTro == userId);
        }

        // GET: api/LoaiPhong?maNhaTro=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoaiPhong>>> GetLoaiPhong([FromQuery] int? maNhaTro = null)
        {
            var query = ApplyRoleFilter(_context.LoaiPhong
                .Include(lp => lp.NhaTro)
                .AsQueryable());

            if (maNhaTro.HasValue)
            {
                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenNhaTro(maNhaTro.Value))
                    return Forbid();

                query = query.Where(lp => lp.MaNhaTro == maNhaTro.Value);
            }

            var data = await query
                .OrderBy(lp => lp.NhaTro != null ? lp.NhaTro.TenNhaTro : "")
                .ThenBy(lp => lp.TenLoaiPhong)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("TheoNhaTro/{maNhaTro}")]
        public async Task<ActionResult<IEnumerable<LoaiPhong>>> GetTheoNhaTro(int maNhaTro)
        {
            return await GetLoaiPhong((int?)maNhaTro);
        }

        // GET: api/LoaiPhong/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LoaiPhong>> GetLoaiPhong(int id)
        {
            var loaiPhong = await ApplyRoleFilter(_context.LoaiPhong
                    .Include(lp => lp.NhaTro)
                    .AsQueryable())
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

            if (!await ChuTroCoQuyenNhaTro(loaiPhong.MaNhaTro))
                return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn nhà trọ hợp lệ trước khi thêm loại phòng"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                loaiPhong.MaChuTro = GetCurrentUserId();
            }
            else if (loaiPhong.MaNhaTro.HasValue)
            {
                loaiPhong.MaChuTro = await _context.NhaTro
                    .Where(n => n.MaNhaTro == loaiPhong.MaNhaTro.Value)
                    .Select(n => n.MaChuTro)
                    .FirstOrDefaultAsync();
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

            if (!await ChuTroCoQuyenNhaTro(loaiPhong.MaNhaTro))
                return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn nhà trọ hợp lệ trước khi cập nhật loại phòng"));

            var existing = await _context.LoaiPhong
                .Include(lp => lp.NhaTro)
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy loại phòng"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                var oldOk = (existing.MaNhaTro != null && existing.NhaTro?.MaChuTro == userId) ||
                            (existing.MaNhaTro == null && existing.MaChuTro == userId);
                if (!oldOk) return Forbid();
            }

            existing.TenLoaiPhong = loaiPhong.TenLoaiPhong;
            existing.MoTa = loaiPhong.MoTa;
            existing.MaNhaTro = loaiPhong.MaNhaTro;

            if (GetCurrentRole() == VaiTroConst.ChuTro)
                existing.MaChuTro = GetCurrentUserId();
            else
                existing.MaChuTro = loaiPhong.MaChuTro ?? await _context.NhaTro.Where(n => n.MaNhaTro == loaiPhong.MaNhaTro).Select(n => n.MaChuTro).FirstOrDefaultAsync();

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/LoaiPhong/5
        // Logic:
        //   Chưa có phòng nào dùng → Xóa cứng
        //   Đã có phòng dùng → Chuyển TrangThai = NgungSuDung
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteLoaiPhong(int id)
        {
            var loaiPhong = await _context.LoaiPhong
                .Include(lp => lp.NhaTro)
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);
            if (loaiPhong == null || loaiPhong.TrangThai == "DaXoa")
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy loại phòng"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                var ok = (loaiPhong.MaNhaTro != null && loaiPhong.NhaTro?.MaChuTro == userId) ||
                         (loaiPhong.MaNhaTro == null && loaiPhong.MaChuTro == userId);
                if (!ok) return Forbid();
            }

            var result = await _deleteValidationService.DeleteLoaiPhongAsync(id);
            return this.ToActionResult(result);

            var coPhong = await _context.Phong.AnyAsync(p => p.MaLoaiPhong == id);

            if (!coPhong)
            {
                // Chưa phát sinh dữ liệu → Xóa cứng
                _context.LoaiPhong.Remove(loaiPhong);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null!, "Đã xóa loại phòng thành công"));
            }
            else
            {
                // Đã có phòng sử dụng → Chuyển trạng thái NgungSuDung
                loaiPhong.TrangThai = "NgungSuDung";
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null!,
                    "Loại phòng đang được sử dụng bởi một hoặc nhiều phòng. " +
                    "Đã chuyển sang trạng thái \"Ngưng sử dụng\"."));
            }
        }
    }
}
