using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.DTOs;
using DoAnSE104.Models.Dtos;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HopDongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HopDongController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetTrangThaiText(DateTime? ngayKetThuc)
        {
            if (ngayKetThuc == null) return "Äang cÃ²n hiá»‡u lá»±c";

            var days = (ngayKetThuc.Value - DateTime.Today).TotalDays;

            if (days < 0) return "Káº¿t thÃºc há»£p Ä‘á»“ng";
            if (days <= 7) return "Sáº¯p háº¿t há»£p Ä‘á»“ng";
            return "Äang cÃ²n hiá»‡u lá»±c";
        }

        // GET: api/HopDong
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetHopDong()
        {
            var hopDongs = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .ToListAsync();

            var result = hopDongs.Select(h => new
            {
                h.MaHopDong,
                h.MaNguoiThue,
                h.MaPhong,
                h.NgayBatDau,
                h.NgayKetThuc,
                h.TienCoc,
                h.NoiDung,
                NguoiThue = new { h.NguoiThue.HoTen },
                Phong = new { h.Phong.TenPhong },
                TrangThaiText = GetTrangThaiText(h.NgayKetThuc)
            });

            return Ok(result);
        }

        // GET: api/HopDong/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetHopDong(int id)
        {
            var h = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHopDong == id);

            if (h == null)
                return NotFound();

            return Ok(new
            {
                h.MaHopDong,
                h.MaNguoiThue,
                h.MaPhong,
                h.NgayBatDau,
                h.NgayKetThuc,
                h.TienCoc,
                h.NoiDung,
                TrangThaiText = GetTrangThaiText(h.NgayKetThuc)
            });
        }

        // GET: api/HopDong/NguoiThue/5
        [HttpGet("NguoiThue/{nguoiThueId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetHopDongByNguoiThue(int nguoiThueId)
        {
            var hopDongs = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .Where(h => h.MaNguoiThue == nguoiThueId)
                .ToListAsync();

            var result = hopDongs.Select(h => new
            {
                h.MaHopDong,
                h.MaNguoiThue,
                h.MaPhong,
                h.NgayBatDau,
                h.NgayKetThuc,
                h.TienCoc,
                h.NoiDung,
                TrangThaiText = GetTrangThaiText(h.NgayKetThuc)
            });

            return Ok(result);
        }

        // GET: api/HopDong/Phong/5
        [HttpGet("Phong/{phongId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetHopDongByPhong(int phongId)
        {
            var hopDongs = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .Where(h => h.MaPhong == phongId)
                .ToListAsync();

            var result = hopDongs.Select(h => new
            {
                h.MaHopDong,
                h.MaNguoiThue,
                h.MaPhong,
                h.NgayBatDau,
                h.NgayKetThuc,
                h.TienCoc,
                h.NoiDung,
                TrangThaiText = GetTrangThaiText(h.NgayKetThuc)
            });

            return Ok(result);
        }

        // GET: api/HopDong/NguoiThue/KhongCoHopDong
        [HttpGet("NguoiThue/KhongCoHopDong")]
        public async Task<ActionResult<IEnumerable<NguoiThue>>> GetNguoiThueChuaCoHopDong()
        {
            var nguoiThueDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaNguoiThue)
                .Distinct()
                .ToListAsync();

            var nguoiThue = await _context.NguoiThue
                .Where(nt => !nguoiThueDaCoHopDong.Contains(nt.MaNguoiThue))
                .ToListAsync();

            return Ok(nguoiThue);
        }

        // GET: api/HopDong/Phong/KhongCoHopDong
        [HttpGet("Phong/KhongCoHopDong")]
        public async Task<ActionResult<IEnumerable<Phong>>> GetPhongChuaCoHopDong()
        {
            var phongDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaPhong)
                .Distinct()
                .ToListAsync();

            var phong = await _context.Phong
                .Where(p => !phongDaCoHopDong.Contains(p.MaPhong))
                .ToListAsync();

            return Ok(phong);
        }

        // POST: api/HopDong
        [HttpPost]
        public async Task<ActionResult<HopDong>> PostHopDong(CreateHopDongDto dto)
        {
            var phongConflict = await _context.HopDong
                .FirstOrDefaultAsync(h => h.MaPhong == dto.MaPhong && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

            if (phongConflict != null)
                return BadRequest("PhÃ²ng nÃ y Ä‘Ã£ cÃ³ há»£p Ä‘á»“ng Ä‘ang hoáº¡t Ä‘á»™ng.");

            var nguoiThueConflict = await _context.HopDong
                .FirstOrDefaultAsync(h => h.MaNguoiThue == dto.MaNguoiThue && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

            if (nguoiThueConflict != null)
                return BadRequest("NgÆ°á»i thuÃª nÃ y Ä‘Ã£ cÃ³ há»£p Ä‘á»“ng Ä‘ang hoáº¡t Ä‘á»™ng.");

            var hopDong = new HopDong
            {
                MaNguoiThue = dto.MaNguoiThue,
                MaPhong = dto.MaPhong,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                TienCoc = dto.TienCoc,
                NoiDung = dto.NoiDung
            };

            _context.HopDong.Add(hopDong);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHopDong), new { id = hopDong.MaHopDong }, hopDong);
        }

        // PUT: api/HopDong/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHopDong(int id, HopDongUpdateDto hopDongDto)
        {
            if (id != hopDongDto.MaHopDong)
                return BadRequest("ID khÃ´ng khá»›p");

            var hopDong = await _context.HopDong.FindAsync(id);
            if (hopDong == null)
                return NotFound();

            // Map cÃ¡c thuá»™c tÃ­nh tá»« DTO sang entity
            hopDong.MaPhong = hopDongDto.MaPhong;
            hopDong.MaNguoiThue = hopDongDto.MaNguoiThue;
            hopDong.NgayBatDau = hopDongDto.NgayBatDau;
            hopDong.NgayKetThuc = hopDongDto.NgayKetThuc;
            hopDong.TienCoc = hopDongDto.TienCoc;
            hopDong.NoiDung = hopDongDto.NoiDung;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HopDongExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }


        // DELETE: api/HopDong/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHopDong(int id)
        {
            var hopDong = await _context.HopDong.FindAsync(id);
            if (hopDong == null)
                return NotFound();

            _context.HopDong.Remove(hopDong);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HopDongExists(int id)
        {
            return _context.HopDong.Any(e => e.MaHopDong == id);
        }

        // GET: api/HopDong/TaoMoi
        [HttpGet("TaoMoi")]
        public async Task<ActionResult> GetNguoiThueVaPhongConTrong()
        {
            var nguoiThueDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaNguoiThue)
                .ToListAsync();

            var phongDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaPhong)
                .ToListAsync();

            var nguoiThue = await _context.NguoiThue
                .Where(nt => !nguoiThueDaCoHopDong.Contains(nt.MaNguoiThue))
                .ToListAsync();

            var phong = await _context.Phong
                .Where(p => !phongDaCoHopDong.Contains(p.MaPhong))
                .ToListAsync();

            return Ok(new { NguoiThue = nguoiThue, Phong = phong });
        }
    }
}

