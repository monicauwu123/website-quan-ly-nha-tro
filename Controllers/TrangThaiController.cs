using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrangThaiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrangThaiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================================
        // GET
        // ================================

        // GET: api/TrangThai
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrangThai>>> GetAll()
        {
            await TaoTrangThaiMacDinhNeuChuaCo();

            var list = await _context.TrangThai
                .OrderBy(t => t.MaTrangThai)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/TrangThai/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrangThai>> GetById(int id)
        {
            var trangThai = await _context.TrangThai.FindAsync(id);

            if (trangThai == null)
            {
                return NotFound("Không tìm thấy trạng thái.");
            }

            return Ok(trangThai);
        }

        // ================================
        // POST
        // ================================

        // POST: api/TrangThai
        [HttpPost]
        public async Task<ActionResult<TrangThai>> Create(TrangThai trangThai)
        {
            _context.TrangThai.Add(trangThai);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = trangThai.MaTrangThai }, trangThai);
        }

        // ================================
        // PUT
        // ================================

        // PUT: api/TrangThai/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TrangThai trangThai)
        {
            if (id != trangThai.MaTrangThai)
            {
                return BadRequest("Mã trạng thái không khớp.");
            }

            _context.Entry(trangThai).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrangThaiExists(id))
                {
                    return NotFound("Không tìm thấy trạng thái cần cập nhật.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // ================================
        // DELETE
        // ================================

        // DELETE: api/TrangThai/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var trangThai = await _context.TrangThai.FindAsync(id);
            if (trangThai == null)
            {
                return NotFound("Không tìm thấy trạng thái cần xóa.");
            }

            _context.TrangThai.Remove(trangThai);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ================================
        // HELPER
        // ================================


        private async Task TaoTrangThaiMacDinhNeuChuaCo()
        {
            if (await _context.TrangThai.AnyAsync())
            {
                return;
            }

            _context.TrangThai.AddRange(
                new TrangThai { TenTrangThai = "Còn trống" },
                new TrangThai { TenTrangThai = "Đã thuê" },
                new TrangThai { TenTrangThai = "Đang sửa chữa" }
            );

            await _context.SaveChangesAsync();
        }

        private bool TrangThaiExists(int id) =>
            _context.TrangThai.Any(e => e.MaTrangThai == id);
    }
}

