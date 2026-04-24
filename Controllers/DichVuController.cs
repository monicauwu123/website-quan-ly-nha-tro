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
    public class DichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DichVuController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private IQueryable<DichVu> ApplyRoleFilter(IQueryable<DichVu> query)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
                query = query.Where(dv => dv.MaChuTro == userId);

            // Người dùng được xem dịch vụ để xem chi phí phòng; Admin xem tất cả.
            return query;
        }

        private async Task<decimal> TongTienDichVuTheoPhongAsync(int maPhong)
        {
            var maChuTro = await _context.Phong
                .Where(p => p.MaPhong == maPhong)
                .Select(p => p.NhaTro.MaChuTro)
                .FirstOrDefaultAsync();

            if (maChuTro == null)
                return 0m;

            return await _context.DichVu
                .Where(dv => dv.MaChuTro == maChuTro.Value)
                .SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;
        }

        [HttpGet("TongTienDichVuTheoPhong")]
        public async Task<ActionResult<decimal>> GetTongTienDichVuTheoPhong([FromQuery] int? maPhong)
        {
            if (maPhong.HasValue)
                return Ok(await TongTienDichVuTheoPhongAsync(maPhong.Value));

            var tongTien = await ApplyRoleFilter(_context.DichVu.AsQueryable())
                .SumAsync(d => (decimal?)d.Tiendichvu) ?? 0m;

            return Ok(tongTien);
        }

        // GET: api/DichVu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DichVu>>> GetDichVu()
        {
            var danhSachDichVu = await ApplyRoleFilter(_context.DichVu.AsQueryable())
                .OrderBy(dv => dv.TenDichVu)
                .ToListAsync();

            return Ok(danhSachDichVu);
        }

        // GET: api/DichVu/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DichVu>> GetDichVu(int id)
        {
            var dichVu = await ApplyRoleFilter(_context.DichVu.AsQueryable())
                .FirstOrDefaultAsync(d => d.MaDichVu == id);

            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            return Ok(dichVu);
        }

        // GET: api/DichVu/5/GiaHienTai
        [HttpGet("{id}/GiaHienTai")]
        public async Task<ActionResult<decimal>> GetGiaHienTai(int id)
        {
            var dichVu = await ApplyRoleFilter(_context.DichVu.AsQueryable())
                .FirstOrDefaultAsync(dv => dv.MaDichVu == id);

            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            var giaHienTai = await _context.LichSuGiaDichVu
                .Where(l => l.MaDichVu == id)
                .OrderByDescending(l => l.NgayHieuLuc)
                .Select(l => (decimal?)l.GiaDichVu)
                .FirstOrDefaultAsync();

            return Ok(giaHienTai ?? (decimal)dichVu.Tiendichvu);
        }

        // POST: api/DichVu
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<DichVu>> PostDichVu(DichVu dichVu)
        {
            if (string.IsNullOrWhiteSpace(dichVu.TenDichVu))
                return BadRequest(ApiResponse<object>.Loi("Tên dịch vụ không được để trống"));

            if (dichVu.Tiendichvu < 0)
                return BadRequest(ApiResponse<object>.Loi("Giá dịch vụ phải lớn hơn hoặc bằng 0"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
                dichVu.MaChuTro = GetCurrentUserId();

            _context.DichVu.Add(dichVu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDichVu), new { id = dichVu.MaDichVu }, dichVu);
        }

        // PUT: api/DichVu/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutDichVu(int id, DichVu dichVu)
        {
            if (id != dichVu.MaDichVu)
                return BadRequest(ApiResponse<object>.Loi("Mã dịch vụ không khớp"));

            if (string.IsNullOrWhiteSpace(dichVu.TenDichVu))
                return BadRequest(ApiResponse<object>.Loi("Tên dịch vụ không được để trống"));

            if (dichVu.Tiendichvu < 0)
                return BadRequest(ApiResponse<object>.Loi("Giá dịch vụ phải lớn hơn hoặc bằng 0"));

            var existing = await _context.DichVu.FindAsync(id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && existing.MaChuTro != GetCurrentUserId())
                return Forbid();

            existing.TenDichVu = dichVu.TenDichVu;
            existing.Tiendichvu = dichVu.Tiendichvu;

            if (GetCurrentRole() == VaiTroConst.Admin)
                existing.MaChuTro = dichVu.MaChuTro;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/DichVu/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteDichVu(int id)
        {
            var dichVu = await _context.DichVu.FindAsync(id);
            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && dichVu.MaChuTro != GetCurrentUserId())
                return Forbid();

            _context.DichVu.Remove(dichVu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/DichVu/5/CapNhatGia
        [HttpPost("{id}/CapNhatGia")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<LichSuGiaDichVu>> CapNhatGiaDichVu(int id, [FromBody] decimal giaMoi)
        {
            if (giaMoi < 0)
                return BadRequest(ApiResponse<object>.Loi("Giá mới không hợp lệ"));

            var dichVu = await _context.DichVu.FindAsync(id);
            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && dichVu.MaChuTro != GetCurrentUserId())
                return Forbid();

            dichVu.Tiendichvu = (float)giaMoi;

            var lichSuGia = new LichSuGiaDichVu
            {
                MaDichVu = id,
                GiaDichVu = giaMoi,
                NgayHieuLuc = DateTime.Now
            };

            _context.LichSuGiaDichVu.Add(lichSuGia);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGiaHienTai), new { id }, lichSuGia);
        }

        // GET: api/DichVu/5/LichSuGia
        [HttpGet("{id}/LichSuGia")]
        public async Task<ActionResult<IEnumerable<LichSuGiaDichVu>>> GetLichSuGia(int id)
        {
            var dichVu = await ApplyRoleFilter(_context.DichVu.AsQueryable())
                .FirstOrDefaultAsync(dv => dv.MaDichVu == id);

            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            var lichSu = await _context.LichSuGiaDichVu
                .Where(l => l.MaDichVu == id)
                .OrderByDescending(l => l.NgayHieuLuc)
                .ToListAsync();

            return Ok(lichSu);
        }
    }
}
