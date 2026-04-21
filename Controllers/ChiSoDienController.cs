using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChiSoDienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChiSoDienController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<string?> ValidateChiSoDien(int maPhong, int soDienCu, int soDienMoi, decimal giaDien, DateTime ngayThangDien, int? maDienBoQua = null)
        {
            if (soDienCu < 0)
                return "Chỉ số điện cũ không được âm";

            if (soDienMoi < 0)
                return "Chỉ số điện mới không được âm";

            if (giaDien < 0)
                return "Giá điện không được âm";

            if (soDienMoi < soDienCu)
                return "Chỉ số điện mới phải lớn hơn hoặc bằng chỉ số điện cũ";

            var phongTonTai = await _context.Phong.AnyAsync(p => p.MaPhong == maPhong);
            if (!phongTonTai)
                return "Phòng không tồn tại";

            var trungThang = await _context.ChiSoDien.AnyAsync(c =>
                c.MaPhong == maPhong &&
                c.NgayThangDien.Month == ngayThangDien.Month &&
                c.NgayThangDien.Year == ngayThangDien.Year &&
                (!maDienBoQua.HasValue || c.MaDien != maDienBoQua.Value));

            if (trungThang)
                return "Đã tồn tại chỉ số điện của phòng này trong tháng đã chọn";

            return null;
        }

        // GET: api/ChiSoDien
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChiSoDien>>> GetChiSoDien()
        {
            return await _context.ChiSoDien
                .Include(c => c.Phong)
                .ToListAsync();
        }

        // GET: api/ChiSoDien/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChiSoDien>> GetChiSoDien(int id)
        {
            var chiSoDien = await _context.ChiSoDien
                .Include(c => c.Phong)
                .FirstOrDefaultAsync(c => c.MaDien == id);

            if (chiSoDien == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            return chiSoDien;
        }

        // GET: api/ChiSoDien/phong/5
        [HttpGet("phong/{maPhong}")]
        public async Task<ActionResult<IEnumerable<ChiSoDien>>> GetChiSoDienByPhong(int maPhong)
        {
            return await _context.ChiSoDien
                .Where(c => c.MaPhong == maPhong)
                .OrderByDescending(c => c.NgayThangDien)
                .ToListAsync();
        }

        // POST: api/ChiSoDien
        [HttpPost]
        public async Task<ActionResult<ChiSoDien>> PostChiSoDien(ChiSoDienDtoCreate dto)
        {
            var loiValidation = await ValidateChiSoDien(dto.MaPhong, dto.SoDienCu, dto.SoDienMoi, dto.GiaDien, dto.NgayThangDien);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            var chiSoDien = new ChiSoDien
            {
                MaPhong = dto.MaPhong,
                SoDienCu = dto.SoDienCu,
                SoDienMoi = dto.SoDienMoi,
                GiaDien = dto.GiaDien,
                NgayThangDien = dto.NgayThangDien,
                TienDien = (dto.SoDienMoi - dto.SoDienCu) * dto.GiaDien
            };

            _context.ChiSoDien.Add(chiSoDien);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChiSoDien), new { id = chiSoDien.MaDien }, chiSoDien);
        }

        // PUT: api/ChiSoDien/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChiSoDien(int id, ChiSoDienDtoUpdate dto)
        {
            if (id != dto.MaDien)
            {
                return BadRequest(ApiResponse<object>.Loi("Mã trên đường dẫn không khớp với mã trong dữ liệu gửi lên"));
            }

            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            var loiValidation = await ValidateChiSoDien(dto.MaPhong, dto.SoDienCu, dto.SoDienMoi, dto.GiaDien, dto.NgayThangDien, id);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            chiSoDien.MaPhong = dto.MaPhong;
            chiSoDien.SoDienCu = dto.SoDienCu;
            chiSoDien.SoDienMoi = dto.SoDienMoi;
            chiSoDien.GiaDien = dto.GiaDien;
            chiSoDien.NgayThangDien = dto.NgayThangDien;
            chiSoDien.TienDien = (dto.SoDienMoi - dto.SoDienCu) * dto.GiaDien;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/ChiSoDien/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChiSoDien(int id)
        {
            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            _context.ChiSoDien.Remove(chiSoDien);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChiSoDienExists(int id)
        {
            return _context.ChiSoDien.Any(e => e.MaDien == id);
        }
    }
}
