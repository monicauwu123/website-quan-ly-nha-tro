using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HoaDonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HoaDonController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<bool> ChuTroCoQuyenPhong(int maPhong)
        {
            var userId = GetCurrentUserId();
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == userId);
        }

        private async Task<bool> ChuTroCoQuyenHoaDon(int maHoaDon)
        {
            var userId = GetCurrentUserId();
            return await _context.HoaDon
                .Join(_context.Phong, h => h.MaPhong, p => p.MaPhong, (h, p) => new { h, p })
                .Join(_context.NhaTro, x => x.p.MaNhaTro, n => n.MaNhaTro, (x, n) => new { x.h, n })
                .AnyAsync(x => x.h.MaHoaDon == maHoaDon && x.n.MaChuTro == userId);
        }

        private async Task<decimal> TongTienDichVuTheoPhongAsync(int maPhong)
        {
            var maChuTro = await _context.Phong
                .Where(p => p.MaPhong == maPhong)
                .Select(p => p.NhaTro.MaChuTro)
                .FirstOrDefaultAsync();

            if (maChuTro == null)
                return 0m;

            return await _context.DichVu
                .Where(dv => dv.MaChuTro == maChuTro.Value)
                .SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;
        }

        private bool TryParseKyHoaDon(string kyHoaDon, out int nam, out int thang)
        {
            nam = 0;
            thang = 0;

            if (string.IsNullOrWhiteSpace(kyHoaDon))
                return false;

            var parts = kyHoaDon.Split('-');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out nam) || !int.TryParse(parts[1], out thang))
                return false;

            return nam >= 1900 && nam <= 9999 && thang >= 1 && thang <= 12;
        }

        private async Task<string?> ValidateHoaDon(int maHoaDon, int maPhong, string kyHoaDon, decimal tongTien)
        {
            if (!TryParseKyHoaDon(kyHoaDon, out _, out _))
                return "Kỳ hóa đơn không hợp lệ. Vui lòng nhập theo định dạng yyyy-MM, ví dụ: 2026-05";

            if (tongTien < 0)
                return "Tổng tiền hóa đơn phải lớn hơn hoặc bằng 0";

            var trungHoaDon = await _context.HoaDon.AnyAsync(hd =>
                hd.MaPhong == maPhong &&
                hd.KyHoaDon == kyHoaDon &&
                hd.MaHoaDon != maHoaDon);

            if (trungHoaDon)
                return $"Đã tồn tại hóa đơn cho phòng này trong kỳ {kyHoaDon}";

            return null;
        }

        // Láº¥y danh sÃ¡ch hÃ³a Ä‘Æ¡n vá»›i thÃ´ng tin phÃ²ng vÃ  ngÆ°á»i thuÃª
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetAllHoaDon()
        {
            var role   = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<HoaDon> query = _context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue);

            if (role == VaiTroConst.ChuTro)
            {
                // ChuTro chỉ thấy hóa đơn của phòng thuộc nhà trọ mình
                var maNhaTroList = await _context.NhaTro
                    .Where(n => n.MaChuTro == userId)
                    .Select(n => n.MaNhaTro)
                    .ToListAsync();
                var maPhongList = await _context.Phong
                    .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                    .Select(p => p.MaPhong)
                    .ToListAsync();
                query = query.Where(h => maPhongList.Contains(h.MaPhong));
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                // NguoiDung chỉ thấy hóa đơn gắn với các hồ sơ khách thuê của chính mình.
                // Một tài khoản có thể thuê nhiều phòng/nhà trọ nên cần lọc theo MaNguoiThue, không lọc theo MaPhong.
                var maNguoiThueCuaUser = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId)
                    .Select(nt => nt.MaNguoiThue)
                    .ToListAsync();
                query = query.Where(h => maNguoiThueCuaUser.Contains(h.MaNguoiThue));
            }
            // Admin: không filter

            var hoaDons = await query
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
                    TienDichVu = _context.DichVu.Where(dv => dv.MaChuTro == h.Phong.NhaTro.MaChuTro).Sum(dv => (decimal?)dv.Tiendichvu) ?? 0,
                    TienPhatSinhKhac = h.TienPhatSinhKhac,
                    TongTien = h.TongTien,
                })
                .ToListAsync();

            return Ok(hoaDons);
        }


        // Láº¥y thÃ´ng tin phÃ²ng Ä‘á»ƒ táº¡o hÃ³a Ä‘Æ¡n
        [HttpGet("GetThongTinPhong/{phongId}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> GetThongTinPhong(int phongId)
        {
            var phong = await _context.Phong.FindAsync(phongId);
            if (phong == null)
                return NotFound(ApiResponse<object>.Loi("Phòng không tồn tại"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(phongId))
                return Forbid();

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
            decimal tongTienDichVuChung = await TongTienDichVuTheoPhongAsync(phongId);

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
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> GetPhongChuaCoHoaDonTrongThang([FromQuery] int thang, [FromQuery] int nam)
        {
            if (thang < 1 || thang > 12 || nam < 1900 || nam > 9999)
                return BadRequest("Tháng năm không hợp lệ");

            var phongDaCoHoaDon = await _context.HoaDon
                .Where(hd => hd.NgayLap.Month == thang && hd.NgayLap.Year == nam)
                .Select(hd => hd.MaPhong)
                .ToListAsync();

            IQueryable<Phong> phongQuery = _context.Phong
                .Where(p => !phongDaCoHoaDon.Contains(p.MaPhong));

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var userId = GetCurrentUserId();
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == userId);
            }

            var phongChuaCoHoaDon = await phongQuery.ToListAsync();

            return Ok(phongChuaCoHoaDon);
        }

        // Táº¡o hÃ³a Ä‘Æ¡n má»›i
        // Tạo hóa đơn mới – Admin & ChuTro
        [HttpPost]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> CreateHoaDon([FromBody] HoaDonCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Kiá»ƒm tra phÃ²ng tá»“n táº¡i
                var phong = await _context.Phong.FindAsync(request.MaPhong);
                if (phong == null)
                    return BadRequest(ApiResponse<object>.Loi("Phòng không tồn tại"));

                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(request.MaPhong))
                    return Forbid();

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

                var loiValidationBanDau = await ValidateHoaDon(0, request.MaPhong, request.KyHoaDon, request.TongTien);
                if (loiValidationBanDau != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidationBanDau));

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
                decimal tongTienDichVuChung = await TongTienDichVuTheoPhongAsync(request.MaPhong);

                // TÃ­nh tá»•ng tiá»n hÃ³a Ä‘Æ¡n
                decimal tongTien = request.TienPhong + request.TienDien + request.TienNuoc +
                                 tongTienDichVuChung + request.TienPhatSinhKhac;

                var loiValidationTongTien = await ValidateHoaDon(0, request.MaPhong, request.KyHoaDon, tongTien);
                if (loiValidationTongTien != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidationTongTien));

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
        // Cập nhật hóa đơn – Admin & ChuTro
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> UpdateHoaDon(int id, [FromBody] HoaDonUpdateDto dto)
        {
            if (id != dto.MaHoaDon)
                return BadRequest("ID khÃ´ng khá»›p.");

            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy hóa đơn"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenHoaDon(id))
                return Forbid();

            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null)
                return BadRequest(ApiResponse<object>.Loi("Phòng không tồn tại"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(dto.MaPhong))
                return Forbid();

            var nguoiThue = await _context.NguoiThue.FindAsync(dto.MaNguoiThue);
            if (nguoiThue == null)
                return BadRequest("Người thuê không tồn tại.");

            // Dá»‹ch vá»¥ Ã¡p dá»¥ng chung
            decimal tongTienDichVuChung = await TongTienDichVuTheoPhongAsync(dto.MaPhong);

            var tongTien = phong.GiaPhong + dto.TienDien + dto.TienNuoc + tongTienDichVuChung + dto.TienPhatSinhKhac;
            var loiValidation = await ValidateHoaDon(id, dto.MaPhong, dto.KyHoaDon, tongTien);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            hoaDon.MaNguoiThue = dto.MaNguoiThue;
            hoaDon.MaPhong = dto.MaPhong;
            hoaDon.NgayLap = dto.NgayLap;
            hoaDon.KyHoaDon = dto.KyHoaDon;
            hoaDon.TongTien = tongTien;

            _context.Entry(hoaDon).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Xoá hóa đơn – Admin & ChuTro
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> DeleteHoaDon(int id)
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenHoaDon(id))
                return Forbid();

            _context.HoaDon.Remove(hoaDon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
