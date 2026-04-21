using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Dtos;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChiSoNuocController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChiSoNuocController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<string?> ValidateChiSoNuoc(int maPhong, int soNuocCu, int soNuocMoi, decimal giaNuoc, DateTime ngayThangNuoc, int? maNuocBoQua = null)
        {
            if (soNuocCu < 0)
                return "Chỉ số nước cũ không được âm";

            if (soNuocMoi < 0)
                return "Chỉ số nước mới không được âm";

            if (giaNuoc < 0)
                return "Giá nước không được âm";

            if (soNuocMoi < soNuocCu)
                return "Chỉ số nước mới phải lớn hơn hoặc bằng chỉ số nước cũ";

            var phongTonTai = await _context.Phong.AnyAsync(p => p.MaPhong == maPhong);
            if (!phongTonTai)
                return "Phòng không tồn tại";

            var trungThang = await _context.ChiSoNuoc.AnyAsync(c =>
                c.MaPhong == maPhong &&
                c.NgayThangNuoc.Month == ngayThangNuoc.Month &&
                c.NgayThangNuoc.Year == ngayThangNuoc.Year &&
                (!maNuocBoQua.HasValue || c.MaNuoc != maNuocBoQua.Value));

            if (trungThang)
                return "Đã tồn tại chỉ số nước của phòng này trong tháng đã chọn";

            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChiSoNuoc>>> GetChiSoNuoc()
        {
            return await _context.ChiSoNuoc
                .Include(c => c.Phong)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChiSoNuoc>> GetChiSoNuoc(int id)
        {
            var chiSoNuoc = await _context.ChiSoNuoc
                .Include(c => c.Phong)
                .FirstOrDefaultAsync(c => c.MaNuoc == id);

            if (chiSoNuoc == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            return chiSoNuoc;
        }

        [HttpGet("phong/{maPhong}")]
        public async Task<ActionResult<IEnumerable<ChiSoNuoc>>> GetChiSoNuocByPhong(int maPhong)
        {
            return await _context.ChiSoNuoc
                .Where(c => c.MaPhong == maPhong)
                .OrderByDescending(c => c.NgayThangNuoc)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<ChiSoNuoc>> PostChiSoNuoc(ChiSoNuocDtoCreate dto)
        {
            var loiValidation = await ValidateChiSoNuoc(dto.MaPhong, dto.SoNuocCu, dto.SoNuocMoi, dto.GiaNuoc, dto.NgayThangNuoc);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            var chiSoNuoc = new ChiSoNuoc
            {
                MaPhong = dto.MaPhong,
                SoNuocCu = dto.SoNuocCu,
                SoNuocMoi = dto.SoNuocMoi,
                GiaNuoc = dto.GiaNuoc,
                NgayThangNuoc = dto.NgayThangNuoc,
                TienNuoc = (dto.SoNuocMoi - dto.SoNuocCu) * dto.GiaNuoc
            };

            _context.ChiSoNuoc.Add(chiSoNuoc);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChiSoNuoc), new { id = chiSoNuoc.MaNuoc }, chiSoNuoc);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutChiSoNuoc(int id, ChiSoNuocDtoUpdate dto)
        {
            if (id != dto.MaNuoc)
            {
                return BadRequest(ApiResponse<object>.Loi("Mã trên đường dẫn không khớp với mã trong dữ liệu gửi lên"));
            }

            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            var loiValidation = await ValidateChiSoNuoc(dto.MaPhong, dto.SoNuocCu, dto.SoNuocMoi, dto.GiaNuoc, dto.NgayThangNuoc, id);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            chiSoNuoc.MaPhong = dto.MaPhong;
            chiSoNuoc.SoNuocCu = dto.SoNuocCu;
            chiSoNuoc.SoNuocMoi = dto.SoNuocMoi;
            chiSoNuoc.GiaNuoc = dto.GiaNuoc;
            chiSoNuoc.NgayThangNuoc = dto.NgayThangNuoc;
            chiSoNuoc.TienNuoc = (dto.SoNuocMoi - dto.SoNuocCu) * dto.GiaNuoc;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChiSoNuoc(int id)
        {
            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
            {
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
            }

            _context.ChiSoNuoc.Remove(chiSoNuoc);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChiSoNuocExists(int id)
        {
            return _context.ChiSoNuoc.Any(e => e.MaNuoc == id);
        }
    }
}
