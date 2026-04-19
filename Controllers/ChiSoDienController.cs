using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChiSoDienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChiSoDienController(ApplicationDbContext context)
        {
            _context = context;
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
                return NotFound();
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
                return BadRequest();
            }

            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien == null)
            {
                return NotFound();
            }

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
                return NotFound();
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

