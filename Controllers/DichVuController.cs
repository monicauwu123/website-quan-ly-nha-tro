using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DichVuController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("TongTienDichVuTheoPhong")]
public async Task<ActionResult<float>> GetTongTienDichVuTheoPhong()
{
    var tongTien = await _context.DichVu.SumAsync(d => d.Tiendichvu);
    return Ok(tongTien);
}


        // GET: api/DichVu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DichVu>>> GetDichVu()
        {
            var danhSachDichVu = await _context.DichVu.ToListAsync();

            if (danhSachDichVu == null || !danhSachDichVu.Any())
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥ nÃ o." });
            }

            return Ok(danhSachDichVu);
        }


        // GET: api/DichVu/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DichVu>> GetDichVu(int id)
        {
            var dichVu = await _context.DichVu
                .FirstOrDefaultAsync(d => d.MaDichVu == id);

            if (dichVu == null)
            {
                return NotFound();
            }

            return Ok(dichVu);
        }

        // GET: api/DichVu/5/GiaHienTai
        [HttpGet("{id}/GiaHienTai")]
        public async Task<ActionResult<decimal>> GetGiaHienTai(int id)
        {
            var giaHienTai = await _context.LichSuGiaDichVu
                .Where(l => l.MaDichVu == id)
                .OrderByDescending(l => l.NgayHieuLuc)
                .Select(l => (decimal?)l.GiaDichVu)
                .FirstOrDefaultAsync();

            if (giaHienTai == null)
            {
                return NotFound("KhÃ´ng tÃ¬m tháº¥y giÃ¡ dá»‹ch vá»¥");
            }

            return Ok(giaHienTai.Value);
        }

        // POST: api/DichVu
        [HttpPost]
        public async Task<ActionResult<DichVu>> PostDichVu(DichVu dichVu)
        {
            _context.DichVu.Add(dichVu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDichVu), new { id = dichVu.MaDichVu }, dichVu);
        }

        // PUT: api/DichVu/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDichVu(int id, DichVu dichVu)
        {
            if (id != dichVu.MaDichVu)
            {
                return BadRequest("ID khÃ´ng khá»›p vá»›i dá»¯ liá»‡u");
            }

            _context.Entry(dichVu).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DichVuExists(id))
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

        // DELETE: api/DichVu/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDichVu(int id)
        {
            var dichVu = await _context.DichVu.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }

            _context.DichVu.Remove(dichVu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/DichVu/5/CapNhatGia
        [HttpPost("{id}/CapNhatGia")]
        public async Task<ActionResult<LichSuGiaDichVu>> CapNhatGiaDichVu(int id, [FromBody] decimal giaMoi)
        {
            if (giaMoi < 0)
            {
                return BadRequest("GiÃ¡ má»›i khÃ´ng há»£p lá»‡");
            }

            var dichVu = await _context.DichVu.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound("KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥");
            }

            var lichSuGia = new LichSuGiaDichVu
            {
                MaDichVu = id,
                GiaDichVu = giaMoi,
                NgayHieuLuc = DateTime.Now
            };

            _context.LichSuGiaDichVu.Add(lichSuGia);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGiaHienTai), new { id = id }, lichSuGia);
        }

        // GET: api/DichVu/5/LichSuGia
        [HttpGet("{id}/LichSuGia")]
        public async Task<ActionResult<IEnumerable<LichSuGiaDichVu>>> GetLichSuGia(int id)
        {
            var lichSu = await _context.LichSuGiaDichVu
                .Where(l => l.MaDichVu == id)
                .OrderByDescending(l => l.NgayHieuLuc)
                .ToListAsync();

            return Ok(lichSu);
        }

        private bool DichVuExists(int id)
        {
            return _context.DichVu.Any(e => e.MaDichVu == id);
        }
    }
}

