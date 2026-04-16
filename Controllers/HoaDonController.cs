using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoaDonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HoaDonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Láº¥y danh sÃ¡ch hÃ³a Ä‘Æ¡n vá»›i thÃ´ng tin phÃ²ng vÃ  ngÆ°á»i thuÃª
        [HttpGet]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetAllHoaDon()
        {
            var hoaDons = await _context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Select(h => new HoaDonDto
                {
                    MaHoaDon = h.MaHoaDon,
                    TenPhong = h.Phong.TenPhong,
                    TenNguoiThue = h.NguoiThue.HoTen,
                    NgayLap = h.NgayLap,
                    KyHoaDon = h.KyHoaDon,

                    TienPhong = h.Phong.GiaPhong, // hoáº·c h.TienPhong náº¿u cÃ³
                    TienDien = h.ChiSoDien.TienDien, // náº¿u cÃ³ liÃªn káº¿t ChiSoDien
                    TienNuoc = h.ChiSoNuoc.TienNuoc, // náº¿u cÃ³ liÃªn káº¿t ChiSoNuoc
                    TienDichVu = _context.DichVu.Sum(dv => (decimal?)dv.Tiendichvu) ?? 0, // cáº§n tinh chá»‰nh
                    TienPhatSinhKhac = h.TienPhatSinhKhac,
                    TongTien = h.TongTien,
                })
                .ToListAsync();

            return Ok(hoaDons);
        }


        // Láº¥y thÃ´ng tin phÃ²ng Ä‘á»ƒ táº¡o hÃ³a Ä‘Æ¡n
        [HttpGet("GetThongTinPhong/{phongId}")]
        public async Task<IActionResult> GetThongTinPhong(int phongId)
        {
            var phong = await _context.Phong.FindAsync(phongId);
            if (phong == null)
                return NotFound("PhÃ²ng khÃ´ng tá»“n táº¡i");

            var hopDong = await _context.HopDong
                .Where(hd => hd.MaPhong == phongId
                      && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now))
                .OrderByDescending(hd => hd.NgayBatDau)
                .FirstOrDefaultAsync();
            if (hopDong == null)
                return BadRequest("PhÃ²ng chÆ°a cÃ³ há»£p Ä‘á»“ng thuÃª há»£p lá»‡");

            var nguoiThue = await _context.NguoiThue.FindAsync(hopDong.MaNguoiThue);
            if (nguoiThue == null)
                return BadRequest("KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i thuÃª");

            var chiSoDien = await _context.ChiSoDien
                .Where(cd => cd.MaPhong == phongId)
                .OrderByDescending(cd => cd.NgayThangDien)
                .FirstOrDefaultAsync();
            if (chiSoDien == null)
                return BadRequest("ChÆ°a cÃ³ chá»‰ sá»‘ Ä‘iá»‡n cho phÃ²ng nÃ y");

            var chiSoNuoc = await _context.ChiSoNuoc
                .Where(cn => cn.MaPhong == phongId)
                .OrderByDescending(cn => cn.NgayThangNuoc)
                .FirstOrDefaultAsync();
            if (chiSoNuoc == null)
                return BadRequest("ChÆ°a cÃ³ chá»‰ sá»‘ nÆ°á»›c cho phÃ²ng nÃ y");

            // Tá»•ng tiá»n dá»‹ch vá»¥ Ã¡p dá»¥ng cho táº¥t cáº£ cÃ¡c phÃ²ng (dá»‹ch vá»¥ cá»‘ Ä‘á»‹nh)
            decimal tongTienDichVuChung = await _context.DichVu.SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;

            // TÃ­nh tá»•ng tiá»n hÃ³a Ä‘Æ¡n
            decimal tongTienHoaDon = phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc + tongTienDichVuChung;

            return Ok(new
            {
                Phong = new { phong.MaPhong, phong.TenPhong, GiaPhong = phong.GiaPhong },
                NguoiThue = new { nguoiThue.MaNguoiThue, nguoiThue.HoTen },
                TienDien = chiSoDien.TienDien,
                TienNuoc = chiSoNuoc.TienNuoc,
                TongTienDichVu = tongTienDichVuChung,
                TongTienHoaDon = tongTienHoaDon,
                // ThÃªm MaDien vÃ  MaNuoc Ä‘á»ƒ táº¡o hÃ³a Ä‘Æ¡n
                MaDien = chiSoDien.MaDien,
                MaNuoc = chiSoNuoc.MaNuoc
            });
        }

        // Láº¥y danh sÃ¡ch phÃ²ng chÆ°a cÃ³ hÃ³a Ä‘Æ¡n trong thÃ¡ng/nÄƒm cá»¥ thá»ƒ
        [HttpGet("GetPhongChuaCoHoaDonTrongThang")]
        public async Task<IActionResult> GetPhongChuaCoHoaDonTrongThang([FromQuery] int thang, [FromQuery] int nam)
        {
            var phongDaCoHoaDon = await _context.HoaDon
                .Where(hd => hd.NgayLap.Month == thang && hd.NgayLap.Year == nam)
                .Select(hd => hd.MaPhong)
                .ToListAsync();

            var phongChuaCoHoaDon = await _context.Phong
                .Where(p => !phongDaCoHoaDon.Contains(p.MaPhong))
                .ToListAsync();

            return Ok(phongChuaCoHoaDon);
        }

        // Táº¡o hÃ³a Ä‘Æ¡n má»›i
        [HttpPost]
        public async Task<IActionResult> CreateHoaDon([FromBody] HoaDonCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Kiá»ƒm tra phÃ²ng tá»“n táº¡i
                var phong = await _context.Phong.FindAsync(request.MaPhong);
                if (phong == null)
                    return BadRequest("PhÃ²ng khÃ´ng tá»“n táº¡i");

                // Kiá»ƒm tra ngÆ°á»i thuÃª tá»“n táº¡i
                var nguoiThue = await _context.NguoiThue.FindAsync(request.MaNguoiThue);
                if (nguoiThue == null)
                    return BadRequest("NgÆ°á»i thuÃª khÃ´ng tá»“n táº¡i");

                // Kiá»ƒm tra há»£p Ä‘á»“ng thuÃª há»£p lá»‡
                var hopDong = await _context.HopDong
                    .Where(hd => hd.MaPhong == request.MaPhong
                           && hd.MaNguoiThue == request.MaNguoiThue
                           && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now))
                    .FirstOrDefaultAsync();

                if (hopDong == null)
                    return BadRequest("KhÃ´ng tÃ¬m tháº¥y há»£p Ä‘á»“ng thuÃª há»£p lá»‡ cho phÃ²ng nÃ y");

                // Kiá»ƒm tra hÃ³a Ä‘Æ¡n Ä‘Ã£ tá»“n táº¡i trong ká»³ nÃ y chÆ°a
                var hoaDonTonTai = await _context.HoaDon
                    .AnyAsync(hd => hd.MaPhong == request.MaPhong
                              && hd.KyHoaDon == request.KyHoaDon);

                if (hoaDonTonTai)
                    return BadRequest($"ÄÃ£ tá»“n táº¡i hÃ³a Ä‘Æ¡n cho phÃ²ng nÃ y trong ká»³ {request.KyHoaDon}");

                // Láº¥y chá»‰ sá»‘ Ä‘iá»‡n vÃ  nÆ°á»›c má»›i nháº¥t cho phÃ²ng
                var chiSoDien = await _context.ChiSoDien
                    .Where(cd => cd.MaPhong == request.MaPhong)
                    .OrderByDescending(cd => cd.NgayThangDien)
                    .FirstOrDefaultAsync();

                if (chiSoDien == null)
                    return BadRequest("ChÆ°a cÃ³ chá»‰ sá»‘ Ä‘iá»‡n cho phÃ²ng nÃ y");

                var chiSoNuoc = await _context.ChiSoNuoc
                    .Where(cn => cn.MaPhong == request.MaPhong)
                    .OrderByDescending(cn => cn.NgayThangNuoc)
                    .FirstOrDefaultAsync();

                if (chiSoNuoc == null)
                    return BadRequest("ChÆ°a cÃ³ chá»‰ sá»‘ nÆ°á»›c cho phÃ²ng nÃ y");

                // Láº¥y tá»•ng tiá»n dá»‹ch vá»¥ chung
                decimal tongTienDichVuChung = await _context.DichVu
                    .SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;

                // TÃ­nh tá»•ng tiá»n hÃ³a Ä‘Æ¡n
                decimal tongTien = request.TienPhong + request.TienDien + request.TienNuoc +
                                 tongTienDichVuChung + request.TienPhatSinhKhac;

                // Táº¡o hÃ³a Ä‘Æ¡n má»›i - QUAN TRá»ŒNG: ThÃªm MaDien vÃ  MaNuoc
                var hoaDon = new HoaDon
                {
                    MaPhong = request.MaPhong,
                    MaNguoiThue = request.MaNguoiThue,
                    MaDien = chiSoDien.MaDien,  // THÃŠM DÃ’NG NÃ€Y
                    MaNuoc = chiSoNuoc.MaNuoc,  // THÃŠM DÃ’NG NÃ€Y
                    NgayLap = request.NgayLap,
                    KyHoaDon = request.KyHoaDon,
                    TongTien = tongTien,
                    TienPhatSinhKhac = request.TienPhatSinhKhac
                };

                _context.HoaDon.Add(hoaDon);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Táº¡o hÃ³a Ä‘Æ¡n thÃ nh cÃ´ng",

                    MaHoaDon = hoaDon.MaHoaDon,
                    TongTien = hoaDon.TongTien,
                    KyHoaDon = hoaDon.KyHoaDon
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lá»—i khi táº¡o hÃ³a Ä‘Æ¡n: {ex.Message}");
            }
        }

        // Cáº­p nháº­t hÃ³a Ä‘Æ¡n
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHoaDon(int id, [FromBody] HoaDonUpdateDto dto)
        {
            if (id != dto.MaHoaDon)
                return BadRequest("ID khÃ´ng khá»›p.");

            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
                return NotFound("KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n.");

            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null)
                return BadRequest("PhÃ²ng khÃ´ng tá»“n táº¡i.");

            // Dá»‹ch vá»¥ Ã¡p dá»¥ng chung
            decimal tongTienDichVuChung = await _context.DichVu.SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;

            hoaDon.MaNguoiThue = dto.MaNguoiThue;
            hoaDon.MaPhong = dto.MaPhong;
            hoaDon.NgayLap = dto.NgayLap;
            hoaDon.KyHoaDon = dto.KyHoaDon;
            hoaDon.TongTien = phong.GiaPhong + dto.TienDien + dto.TienNuoc + tongTienDichVuChung + dto.TienPhatSinhKhac;

            _context.Entry(hoaDon).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // XoÃ¡ hÃ³a Ä‘Æ¡n
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoaDon(int id)
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
                return NotFound();

            _context.HoaDon.Remove(hoaDon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
