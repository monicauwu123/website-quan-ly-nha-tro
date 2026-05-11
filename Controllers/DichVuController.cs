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
    public class DichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDeleteValidationService _deleteValidationService;

        public DichVuController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private static string ChuanHoaLoaiDichVu(string? loaiDichVu)
        {
            if (string.Equals(loaiDichVu, "TienIch", StringComparison.OrdinalIgnoreCase))
                return "TienIch";
            if (string.Equals(loaiDichVu, "TienNghi", StringComparison.OrdinalIgnoreCase))
                return "TienNghi";
            return "TinhPhi";
        }

        private IQueryable<DichVu> ApplyRoleFilter(IQueryable<DichVu> query)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
            {
                query = query.Where(dv =>
                    (dv.MaNhaTro != null && dv.NhaTro!.MaChuTro == userId) ||
                    (dv.MaNhaTro == null && dv.MaChuTro == userId));
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

        private async Task<decimal> TongTienDichVuTheoPhongAsync(int maPhong)
        {
            var maNhaTro = await _context.Phong
                .Where(p => p.MaPhong == maPhong)
                .Select(p => (int?)p.MaNhaTro)
                .FirstOrDefaultAsync();

            if (maNhaTro == null)
                return 0m;

            return await _context.DichVu
                .Where(dv => dv.MaNhaTro == maNhaTro.Value)
                .SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;
        }

        [HttpGet("TongTienDichVuTheoPhong")]
        public async Task<ActionResult<decimal>> GetTongTienDichVuTheoPhong([FromQuery] int? maPhong)
        {
            if (maPhong.HasValue)
                return Ok(await TongTienDichVuTheoPhongAsync(maPhong.Value));

            var tongTien = await ApplyRoleFilter(_context.DichVu.Include(d => d.NhaTro).AsQueryable())
                .SumAsync(d => (decimal?)d.Tiendichvu) ?? 0m;

            return Ok(tongTien);
        }

        // GET: api/DichVu?maNhaTro=1&loaiDichVu=TinhPhi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DichVu>>> GetDichVu([FromQuery] int? maNhaTro = null, [FromQuery] string? loaiDichVu = null)
        {
            var query = ApplyRoleFilter(_context.DichVu
                .Include(dv => dv.NhaTro)
                .AsQueryable());

            if (maNhaTro.HasValue)
            {
                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenNhaTro(maNhaTro.Value))
                    return Forbid();

                query = query.Where(dv => dv.MaNhaTro == maNhaTro.Value);
            }

            if (!string.IsNullOrWhiteSpace(loaiDichVu))
            {
                var loai = ChuanHoaLoaiDichVu(loaiDichVu);
                query = query.Where(dv => dv.LoaiDichVu == loai);
            }

            var danhSachDichVu = await query
                .OrderBy(dv => dv.NhaTro != null ? dv.NhaTro.TenNhaTro : "")
                .ThenBy(dv => dv.LoaiDichVu)
                .ThenBy(dv => dv.TenDichVu)
                .ToListAsync();

            return Ok(danhSachDichVu);
        }

        [HttpGet("TheoNhaTro/{maNhaTro}")]
        public async Task<ActionResult<IEnumerable<DichVu>>> GetTheoNhaTro(int maNhaTro)
        {
            return await GetDichVu((int?)maNhaTro, null);
        }

        // GET: api/DichVu/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DichVu>> GetDichVu(int id)
        {
            var dichVu = await ApplyRoleFilter(_context.DichVu
                    .Include(dv => dv.NhaTro)
                    .AsQueryable())
                .FirstOrDefaultAsync(d => d.MaDichVu == id);

            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            return Ok(dichVu);
        }

        // GET: api/DichVu/5/GiaHienTai
        [HttpGet("{id}/GiaHienTai")]
        public async Task<ActionResult<decimal>> GetGiaHienTai(int id)
        {
            var dichVu = await ApplyRoleFilter(_context.DichVu.Include(d => d.NhaTro).AsQueryable())
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

            dichVu.LoaiDichVu = ChuanHoaLoaiDichVu(dichVu.LoaiDichVu);

            if (!await ChuTroCoQuyenNhaTro(dichVu.MaNhaTro))
                return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn nhà trọ hợp lệ trước khi thêm dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
                dichVu.MaChuTro = GetCurrentUserId();
            else if (dichVu.MaNhaTro.HasValue)
                dichVu.MaChuTro = await _context.NhaTro.Where(n => n.MaNhaTro == dichVu.MaNhaTro.Value).Select(n => n.MaChuTro).FirstOrDefaultAsync();

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

            dichVu.LoaiDichVu = ChuanHoaLoaiDichVu(dichVu.LoaiDichVu);

            if (!await ChuTroCoQuyenNhaTro(dichVu.MaNhaTro))
                return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn nhà trọ hợp lệ trước khi cập nhật dịch vụ"));

            var existing = await _context.DichVu
                .Include(dv => dv.NhaTro)
                .FirstOrDefaultAsync(dv => dv.MaDichVu == id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                var oldOk = (existing.MaNhaTro != null && existing.NhaTro?.MaChuTro == userId) ||
                            (existing.MaNhaTro == null && existing.MaChuTro == userId);
                if (!oldOk) return Forbid();
            }

            existing.TenDichVu = dichVu.TenDichVu;
            existing.Tiendichvu = dichVu.Tiendichvu;
            existing.LoaiDichVu = dichVu.LoaiDichVu;
            existing.MaNhaTro = dichVu.MaNhaTro;

            if (GetCurrentRole() == VaiTroConst.ChuTro)
                existing.MaChuTro = GetCurrentUserId();
            else
                existing.MaChuTro = dichVu.MaChuTro ?? await _context.NhaTro.Where(n => n.MaNhaTro == dichVu.MaNhaTro).Select(n => n.MaChuTro).FirstOrDefaultAsync();

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/DichVu/5
        // Logic:
        //   Chưa có đăng ký dịch vụ / chi tiết hóa đơn → Xóa cứng
        //   Đã có → Chuyển TrangThai = NgungSuDung
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteDichVu(int id)
        {
            var dichVu = await _context.DichVu
                .Include(dv => dv.NhaTro)
                .FirstOrDefaultAsync(dv => dv.MaDichVu == id);
            if (dichVu == null || dichVu.TrangThai == "DaXoa")
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                var ok = (dichVu.MaNhaTro != null && dichVu.NhaTro?.MaChuTro == userId) ||
                         (dichVu.MaNhaTro == null && dichVu.MaChuTro == userId);
                if (!ok) return Forbid();
            }

            // Kiểm tra dữ liệu liên quan
            var result = await _deleteValidationService.DeleteDichVuAsync(id);
            return this.ToActionResult(result);

            var coDangKy    = await _context.DangKyDichVu.AnyAsync(dk => dk.MaDichVu == id);
            var coLichSuGia = await _context.LichSuGiaDichVu.AnyAsync(l => l.MaDichVu == id);

            if (!coDangKy && !coLichSuGia)
            {
                // Chưa phát sinh dữ liệu → Xóa cứng
                _context.DichVu.Remove(dichVu);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null!, "Đã xóa dịch vụ thành công"));
            }
            else
            {
                // Đã có dữ liệu → Chuyển trạng thái NgungSuDung
                dichVu.TrangThai = "NgungSuDung";
                await _context.SaveChangesAsync();

                var lyDo = new List<string>();
                if (coDangKy)    lyDo.Add("đăng ký dịch vụ của khách");
                if (coLichSuGia) lyDo.Add("lịch sử giá");

                return Ok(ApiResponse<object>.Ok(null!,
                    $"Dịch vụ đã có {string.Join(", ", lyDo)} liên quan. " +
                    "Đã chuyển sang trạng thái \"Ngưng sử dụng\"."));
            }
        }

        // POST: api/DichVu/5/CapNhatGia
        [HttpPost("{id}/CapNhatGia")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<LichSuGiaDichVu>> CapNhatGiaDichVu(int id, [FromBody] decimal giaMoi)
        {
            if (giaMoi < 0)
                return BadRequest(ApiResponse<object>.Loi("Giá mới không hợp lệ"));

            var dichVu = await _context.DichVu.Include(d => d.NhaTro).FirstOrDefaultAsync(d => d.MaDichVu == id);
            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                var ok = (dichVu.MaNhaTro != null && dichVu.NhaTro?.MaChuTro == userId) ||
                         (dichVu.MaNhaTro == null && dichVu.MaChuTro == userId);
                if (!ok) return Forbid();
            }

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
            var dichVu = await ApplyRoleFilter(_context.DichVu.Include(d => d.NhaTro).AsQueryable())
                .FirstOrDefaultAsync(dv => dv.MaDichVu == id);

            if (dichVu == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dịch vụ"));

            var lichSuGia = await _context.LichSuGiaDichVu
                .Where(l => l.MaDichVu == id)
                .OrderByDescending(l => l.NgayHieuLuc)
                .ToListAsync();

            return Ok(lichSuGia);
        }
    }
}
