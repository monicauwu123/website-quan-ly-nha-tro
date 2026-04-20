using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
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
                return NotFound();
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
                return BadRequest();
            }

            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
            {
                return NotFound();
            }

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
                return NotFound();
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

