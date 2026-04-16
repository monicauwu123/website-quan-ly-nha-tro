using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThanhToanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThanhToanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ThanhToan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToan()
        {
            return await _context.ThanhToan
                .Include(t => t.HoaDon)
                .ToListAsync();
        }

        // GET: api/ThanhToan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThanhToan>> GetThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan
                .Include(t => t.HoaDon)
                .FirstOrDefaultAsync(t => t.MaThanhToan == id);

            if (thanhToan == null)
            {
                return NotFound();
            }

            return thanhToan;
        }

        // GET: api/ThanhToan/HoaDon/5
        [HttpGet("HoaDon/{hoaDonId}")]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToanByHoaDon(int hoaDonId)
        {
            return await _context.ThanhToan
                .Include(t => t.HoaDon)
                .Where(t => t.MaHoaDon == hoaDonId)
                .ToListAsync();
        }

        // POST: api/ThanhToan
        [HttpPost]
        public async Task<ActionResult<ThanhToan>> PostThanhToan(ThanhToan thanhToan)
        {
            // Kiá»ƒm tra hÃ³a Ä‘Æ¡n tá»“n táº¡i
            var hoaDon = await _context.HoaDon.FindAsync(thanhToan.MaHoaDon);
            if (hoaDon == null)
            {
                return BadRequest("KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n");
            }

            // Kiá»ƒm tra sá»‘ tiá»n thanh toÃ¡n
            var tongDaThanhToan = await _context.ThanhToan
                .Where(t => t.MaHoaDon == thanhToan.MaHoaDon)
                .SumAsync(t => t.TongTien);

            if (tongDaThanhToan + thanhToan.TongTien > hoaDon.TongTien)
            {
                return BadRequest("Sá»‘ tiá»n thanh toÃ¡n vÆ°á»£t quÃ¡ tá»•ng tiá»n hÃ³a Ä‘Æ¡n");
            }

            thanhToan.NgayThanhToan = DateTime.Now;
            _context.ThanhToan.Add(thanhToan);

            // Cáº­p nháº­t tráº¡ng thÃ¡i hÃ³a Ä‘Æ¡n
            if (tongDaThanhToan + thanhToan.TongTien >= hoaDon.TongTien)
            {
                var trangThaiDaThanhToan = await _context.TrangThai.FirstOrDefaultAsync(t => t.TenTrangThai == "ÄÃ£ thanh toÃ¡n");
                if (trangThaiDaThanhToan != null)
                {
                    
                    _context.Entry(hoaDon).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetThanhToan), new { id = thanhToan.MaThanhToan }, thanhToan);
        }

        // PUT: api/ThanhToan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutThanhToan(int id, ThanhToan thanhToan)
        {
            if (id != thanhToan.MaThanhToan)
            {
                return BadRequest();
            }

            _context.Entry(thanhToan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThanhToanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ThanhToan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan.FindAsync(id);
            if (thanhToan == null)
            {
                return NotFound();
            }

            // Cáº­p nháº­t láº¡i tráº¡ng thÃ¡i hÃ³a Ä‘Æ¡n
            var hoaDon = await _context.HoaDon.FindAsync(thanhToan.MaHoaDon);
            if (hoaDon != null)
            {
                var tongDaThanhToan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == thanhToan.MaHoaDon && t.MaThanhToan != id)
                    .SumAsync(t => t.TongTien);

                if (tongDaThanhToan < hoaDon.TongTien)
                {
                    var trangThaiChuaThanhToan = await _context.TrangThai.FirstOrDefaultAsync(t => t.TenTrangThai == "ChÆ°a thanh toÃ¡n");
                    if (trangThaiChuaThanhToan != null)
                    {
                      
                        _context.Entry(hoaDon).State = EntityState.Modified;
                    }
                }
            }

            _context.ThanhToan.Remove(thanhToan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ThanhToanExists(int id)
        {
            return _context.ThanhToan.Any(e => e.MaThanhToan == id);
        }
    }
} 
