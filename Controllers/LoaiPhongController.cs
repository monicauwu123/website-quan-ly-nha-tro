using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnSE104.Helpers;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoaiPhongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LoaiPhong
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoaiPhong>>> GetLoaiPhong()
        {
            return await _context.LoaiPhong.ToListAsync();
        }

        // GET: api/LoaiPhong/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LoaiPhong>> GetLoaiPhong(int id)
        {
            var loaiPhong = await _context.LoaiPhong.FindAsync(id);

            if (loaiPhong == null)
            {
                return NotFound();
            }

            return loaiPhong;
        }

        // POST: api/LoaiPhong
        [HttpPost]
        public async Task<ActionResult<LoaiPhong>> PostLoaiPhong(LoaiPhong loaiPhong)
        {
            _context.LoaiPhong.Add(loaiPhong);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLoaiPhong), new { id = loaiPhong.MaLoaiPhong }, loaiPhong);
        }

        // PUT: api/LoaiPhong/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLoaiPhong(int id, LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
            {
                return BadRequest();
            }

            _context.Entry(loaiPhong).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoaiPhongExists(id))
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

        // DELETE: api/LoaiPhong/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoaiPhong(int id)
        {
            var loaiPhong = await _context.LoaiPhong.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            _context.LoaiPhong.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LoaiPhongExists(int id)
        {
            return _context.LoaiPhong.Any(e => e.MaLoaiPhong == id);
        }
    }
} 
