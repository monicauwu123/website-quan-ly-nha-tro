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
    public class ThanhToanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThanhToanController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<List<int>> GetMaPhongCuaChuTro(int userId)
        {
            var maNhaTroList = await _context.NhaTro
                .Where(n => n.MaChuTro == userId)
                .Select(n => n.MaNhaTro)
                .ToListAsync();

            return await _context.Phong
                .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                .Select(p => p.MaPhong)
                .ToListAsync();
        }

        private async Task<bool> CoQuyenThanhToan(ThanhToan thanhToan)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.Admin)
                return true;

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                var maPhong = await _context.HoaDon
                    .Where(h => h.MaHoaDon == thanhToan.MaHoaDon)
                    .Select(h => h.MaPhong)
                    .FirstOrDefaultAsync();

                return maPhongList.Contains(maPhong);
            }

            return await _context.NguoiThue
                .AnyAsync(nt => nt.MaNguoiThue == thanhToan.MaNguoiThue && nt.MaNguoiDung == userId);
        }

        private async Task<string?> ValidateThanhToan(ThanhToan thanhToan)
        {
            if (thanhToan.TongTien < 0)
                return "Số tiền thanh toán phải lớn hơn hoặc bằng 0";

            var hoaDon = await _context.HoaDon.FindAsync(thanhToan.MaHoaDon);
            if (hoaDon == null)
                return "Không tìm thấy hóa đơn";

            var nguoiThue = await _context.NguoiThue.FindAsync(thanhToan.MaNguoiThue);
            if (nguoiThue == null)
                return "Không tìm thấy người thuê";

            if (hoaDon.MaNguoiThue != thanhToan.MaNguoiThue)
                return "Người thuê không khớp với hóa đơn";

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(GetCurrentUserId());
                if (!maPhongList.Contains(hoaDon.MaPhong))
                    return "Bạn chỉ được thao tác thanh toán thuộc nhà trọ của mình";
            }

            var tongDaThanhToan = await _context.ThanhToan
                .Where(t => t.MaHoaDon == thanhToan.MaHoaDon && t.MaThanhToan != thanhToan.MaThanhToan)
                .SumAsync(t => t.TongTien);

            if (tongDaThanhToan + thanhToan.TongTien > hoaDon.TongTien)
                return "Số tiền thanh toán vượt quá tổng tiền hóa đơn";

            return null;
        }

        // GET: api/ThanhToan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToan()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<ThanhToan> query = _context.ThanhToan.Include(t => t.HoaDon);

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                query = query.Where(t => maPhongList.Contains(t.HoaDon.MaPhong));
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                query = query.Where(t => t.NguoiThue.MaNguoiDung == userId);
            }

            return await query.ToListAsync();
        }

        // GET: api/ThanhToan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThanhToan>> GetThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan
                .Include(t => t.HoaDon)
                .FirstOrDefaultAsync(t => t.MaThanhToan == id);

            if (thanhToan == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(thanhToan))
                return Forbid();

            return thanhToan;
        }

        // GET: api/ThanhToan/HoaDon/5
        [HttpGet("HoaDon/{hoaDonId}")]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToanByHoaDon(int hoaDonId)
        {
            var hoaDon = await _context.HoaDon.FindAsync(hoaDonId);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy hóa đơn"));

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                if (!maPhongList.Contains(hoaDon.MaPhong))
                    return Forbid();
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                var coQuyen = await _context.NguoiThue
                    .AnyAsync(nt => nt.MaNguoiThue == hoaDon.MaNguoiThue && nt.MaNguoiDung == userId);
                if (!coQuyen) return Forbid();
            }

            return await _context.ThanhToan
                .Include(t => t.HoaDon)
                .Where(t => t.MaHoaDon == hoaDonId)
                .ToListAsync();
        }

        // POST: api/ThanhToan
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<ThanhToan>> PostThanhToan(ThanhToan thanhToan)
        {
            var loiValidation = await ValidateThanhToan(thanhToan);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            thanhToan.NgayThanhToan = DateTime.Now;
            _context.ThanhToan.Add(thanhToan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetThanhToan), new { id = thanhToan.MaThanhToan },
                ApiResponse<ThanhToan>.Ok(thanhToan, "Thêm thanh toán thành công"));
        }

        // PUT: api/ThanhToan/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutThanhToan(int id, ThanhToan thanhToan)
        {
            if (id != thanhToan.MaThanhToan)
                return BadRequest(ApiResponse<object>.Loi("Mã thanh toán không khớp"));

            var existing = await _context.ThanhToan.AsNoTracking().FirstOrDefaultAsync(t => t.MaThanhToan == id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(existing))
                return Forbid();

            var loiValidation = await ValidateThanhToan(thanhToan);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            _context.Entry(thanhToan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThanhToanExists(id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));
                throw;
            }

            return Ok(ApiResponse<ThanhToan>.Ok(thanhToan, "Cập nhật thanh toán thành công"));
        }

        // DELETE: api/ThanhToan/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan.FindAsync(id);
            if (thanhToan == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(thanhToan))
                return Forbid();

            _context.ThanhToan.Remove(thanhToan);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null!, "Xóa thanh toán thành công"));
        }

        private bool ThanhToanExists(int id)
        {
            return _context.ThanhToan.Any(e => e.MaThanhToan == id);
        }
    }
}
