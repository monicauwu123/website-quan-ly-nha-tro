using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.DTOs;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HopDongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HopDongController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<string?> ValidateHopDong(int maHopDong, int maNguoiThue, int maPhong, DateTime ngayBatDau, DateTime? ngayKetThuc, decimal tienCoc)
        {
            if (ngayKetThuc.HasValue && ngayKetThuc.Value <= ngayBatDau)
                return "Ngày kết thúc phải lớn hơn ngày bắt đầu";

            if (tienCoc < 0)
                return "Tiền cọc phải lớn hơn hoặc bằng 0";

            var nguoiThue = await _context.NguoiThue.FindAsync(maNguoiThue);
            if (nguoiThue == null)
                return "Người thuê không tồn tại";

            var phong = await _context.Phong.FindAsync(maPhong);
            if (phong == null)
                return "Phòng không tồn tại";

            if (phong.GiaPhong <= 0)
                return "Giá thuê phải lớn hơn 0";

            var phongDangCoHopDongHieuLuc = await _context.HopDong.AnyAsync(h =>
                h.MaPhong == maPhong &&
                h.MaHopDong != maHopDong &&
                (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

            if (phongDangCoHopDongHieuLuc)
                return "Phòng này đã có hợp đồng còn hiệu lực";

            return null;
        }

        private string GetTrangThaiText(DateTime? ngayKetThuc)
        {
            if (ngayKetThuc == null) return "Đang còn hiệu lực";

            var days = (ngayKetThuc.Value - DateTime.Today).TotalDays;

            if (days < 0) return "Kết thúc hợp đồng";
            if (days <= 7) return "Sắp hết hợp đồng";
            return "Đang còn hiệu lực";
        }

        // GET: api/HopDong
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetHopDong()
        {
            var role   = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<HopDong> query = _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong);

            if (role == VaiTroConst.ChuTro)
            {
                var maNhaTroList = await _context.NhaTro
                    .Where(n => n.MaChuTro == userId).Select(n => n.MaNhaTro).ToListAsync();
                var maPhongList = await _context.Phong
                    .Where(p => maNhaTroList.Contains(p.MaNhaTro)).Select(p => p.MaPhong).ToListAsync();
                query = query.Where(h => maPhongList.Contains(h.MaPhong));
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                // NguoiDung chỉ thấy hợp đồng của chính mình
                var maPhongCuaUser = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId).Select(nt => nt.MaPhong).ToListAsync();
                query = query.Where(h => maPhongCuaUser.Contains(h.MaPhong));
            }

            var hopDongs = await query.ToListAsync();

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
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

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
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<ActionResult<HopDong>> PostHopDong(CreateHopDongDto dto)
        {
            var loiValidation = await ValidateHopDong(0, dto.MaNguoiThue, dto.MaPhong, dto.NgayBatDau, dto.NgayKetThuc, dto.TienCoc);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            var nguoiThueConflict = await _context.HopDong
                .FirstOrDefaultAsync(h => h.MaNguoiThue == dto.MaNguoiThue && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

            if (nguoiThueConflict != null)
                return BadRequest(ApiResponse<object>.Loi("Người thuê này đã có hợp đồng đang hoạt động."));

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            var nguoiThueCanDongBo = await _context.NguoiThue.FindAsync(dto.MaNguoiThue);

            if (role == VaiTroConst.ChuTro)
            {
                var phongThuocChuTro = await _context.Phong
                    .Include(p => p.NhaTro)
                    .AnyAsync(p => p.MaPhong == dto.MaPhong && p.NhaTro.MaChuTro == userId);

                if (!phongThuocChuTro)
                    return Forbid();
            }

            if (nguoiThueCanDongBo == null)
                return BadRequest(ApiResponse<object>.Loi("Người thuê không tồn tại"));

            // Đồng bộ hồ sơ khách thuê với phòng trong hợp đồng.
            // Nếu khách thuê đã liên kết tài khoản người dùng thì dashboard người dùng sẽ thấy hợp đồng này.
            nguoiThueCanDongBo.MaPhong = dto.MaPhong;

            if (!nguoiThueCanDongBo.MaNguoiDung.HasValue)
            {
                var linkedUser = await _context.Users.FirstOrDefaultAsync(u =>
                    (!string.IsNullOrWhiteSpace(nguoiThueCanDongBo.Email) && u.Email == nguoiThueCanDongBo.Email) ||
                    (!string.IsNullOrWhiteSpace(nguoiThueCanDongBo.SDT) && u.SoDienThoai == nguoiThueCanDongBo.SDT));

                if (linkedUser != null)
                    nguoiThueCanDongBo.MaNguoiDung = linkedUser.MaNguoiDung;
            }

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

            var phongCanCapNhat = await _context.Phong.FindAsync(dto.MaPhong);
            if (phongCanCapNhat != null)
            {
                phongCanCapNhat.SoNguoiHienTai = Math.Min(phongCanCapNhat.SoNguoiHienTai + 1, phongCanCapNhat.SucChua);

                var trangThaiDaThue = await _context.TrangThai
                    .FirstOrDefaultAsync(t => t.TenTrangThai.Contains("Đã thuê") || t.TenTrangThai.Contains("Da thue") || t.TenTrangThai.Contains("thuê"));
                if (trangThaiDaThue != null)
                    phongCanCapNhat.MaTrangThai = trangThaiDaThue.MaTrangThai;
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHopDong), new { id = hopDong.MaHopDong }, ApiResponse<HopDong>.Ok(hopDong, "Tạo hợp đồng thành công"));
        }

        // PUT: api/HopDong/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> PutHopDong(int id, HopDongUpdateDto hopDongDto)
        {
            if (id != hopDongDto.MaHopDong)
                return BadRequest("ID khÃ´ng khá»›p");

            var hopDong = await _context.HopDong.FindAsync(id);
            if (hopDong == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            if (role == VaiTroConst.ChuTro)
            {
                var phongCuThuocChuTro = await _context.Phong
                    .Include(p => p.NhaTro)
                    .AnyAsync(p => p.MaPhong == hopDong.MaPhong && p.NhaTro.MaChuTro == userId);
                var phongMoiThuocChuTro = await _context.Phong
                    .Include(p => p.NhaTro)
                    .AnyAsync(p => p.MaPhong == hopDongDto.MaPhong && p.NhaTro.MaChuTro == userId);

                if (!phongCuThuocChuTro || !phongMoiThuocChuTro)
                    return Forbid();
            }

            var loiValidation = await ValidateHopDong(id, hopDongDto.MaNguoiThue, hopDongDto.MaPhong, hopDongDto.NgayBatDau, hopDongDto.NgayKetThuc, hopDongDto.TienCoc);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            // Map cÃ¡c thuá»™c tÃ­nh tá»« DTO sang entity
            hopDong.MaPhong = hopDongDto.MaPhong;
            hopDong.MaNguoiThue = hopDongDto.MaNguoiThue;
            hopDong.NgayBatDau = hopDongDto.NgayBatDau;
            hopDong.NgayKetThuc = hopDongDto.NgayKetThuc;
            hopDong.TienCoc = hopDongDto.TienCoc;
            hopDong.NoiDung = hopDongDto.NoiDung;

            var nguoiThueCanDongBo = await _context.NguoiThue.FindAsync(hopDongDto.MaNguoiThue);
            if (nguoiThueCanDongBo != null)
                nguoiThueCanDongBo.MaPhong = hopDongDto.MaPhong;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HopDongExists(id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));
                else
                    throw;
            }

            return NoContent();
        }


        // DELETE: api/HopDong/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteHopDong(int id)
        {
            var hopDong = await _context.HopDong.FindAsync(id);
            if (hopDong == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

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
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var nguoiThueDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaNguoiThue)
                .ToListAsync();

            var phongDaCoHopDong = await _context.HopDong
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now)
                .Select(h => h.MaPhong)
                .ToListAsync();

            IQueryable<NguoiThue> nguoiThueQuery = _context.NguoiThue
                .Where(nt => !nguoiThueDaCoHopDong.Contains(nt.MaNguoiThue));

            IQueryable<Phong> phongQuery = _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => !phongDaCoHopDong.Contains(p.MaPhong));

            if (role == VaiTroConst.ChuTro)
            {
                nguoiThueQuery = nguoiThueQuery.Where(nt => _context.Phong.Any(p => p.MaPhong == nt.MaPhong && p.NhaTro.MaChuTro == userId));
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == userId);
            }

            var nguoiThue = await nguoiThueQuery.ToListAsync();
            var phong = await phongQuery.ToListAsync();

            return Ok(new { NguoiThue = nguoiThue, Phong = phong });
        }
    }
}

