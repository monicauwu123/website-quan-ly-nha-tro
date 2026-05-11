using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChiSoDienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDeleteValidationService _deleteValidationService;

        public ChiSoDienController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<bool> ChuTroCoQuyenPhong(int maPhong)
        {
            var userId = GetCurrentUserId();
            return await _context.Phong.AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == userId);
        }

        private async Task<bool> NguoiDungCoQuyenPhong(int maPhong)
        {
            var userId = GetCurrentUserId();
            return await _context.NguoiThue.AnyAsync(nt => nt.MaNguoiDung == userId && nt.MaPhong == maPhong);
        }

        private IQueryable<ChiSoDien> ApplyRoleFilter(IQueryable<ChiSoDien> query)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
                query = query.Where(c => c.Phong.NhaTro.MaChuTro == userId);
            else if (role == VaiTroConst.NguoiDung)
                query = query.Where(c => _context.NguoiThue.Any(nt => nt.MaNguoiDung == userId && nt.MaPhong == c.MaPhong));

            return query;
        }

        private async Task<string?> ValidateChiSoDien(int maPhong, int soDienCu, int soDienMoi, decimal giaDien, DateTime ngayThangDien, int? maDienBoQua = null)
        {
            if (soDienCu < 0)
                return "Chỉ số điện cũ không được âm";

            if (soDienMoi < 0)
                return "Chỉ số điện mới không được âm";

            if (giaDien < 0)
                return "Giá điện không được âm";

            if (soDienMoi < soDienCu)
                return "Chỉ số điện mới phải lớn hơn hoặc bằng chỉ số điện cũ";

            var phongTonTai = await _context.Phong.AnyAsync(p => p.MaPhong == maPhong);
            if (!phongTonTai)
                return "Phòng không tồn tại";

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(maPhong))
                return "Bạn không có quyền ghi chỉ số điện cho phòng này";

            var trungThang = await _context.ChiSoDien.AnyAsync(c =>
                c.MaPhong == maPhong &&
                c.NgayThangDien.Month == ngayThangDien.Month &&
                c.NgayThangDien.Year == ngayThangDien.Year &&
                (!maDienBoQua.HasValue || c.MaDien != maDienBoQua.Value));

            if (trungThang)
                return "Đã tồn tại chỉ số điện của phòng này trong tháng đã chọn";

            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChiSoDien>>> GetChiSoDien()
        {
            return await ApplyRoleFilter(_context.ChiSoDien.Include(c => c.Phong).AsQueryable())
                .OrderByDescending(c => c.NgayThangDien)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChiSoDien>> GetChiSoDien(int id)
        {
            var chiSoDien = await ApplyRoleFilter(_context.ChiSoDien.Include(c => c.Phong).AsQueryable())
                .FirstOrDefaultAsync(c => c.MaDien == id);

            if (chiSoDien == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            return chiSoDien;
        }


        [HttpGet("nha-tro/{maNhaTro}")]
        public async Task<ActionResult<IEnumerable<ChiSoDien>>> GetChiSoDienByNhaTro(int maNhaTro)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var nhaTroTonTai = await _context.NhaTro.AnyAsync(n => n.MaNhaTro == maNhaTro);
            if (!nhaTroTonTai)
                return NotFound(ApiResponse<object>.Loi("Nhà trọ không tồn tại"));

            if (role == VaiTroConst.ChuTro)
            {
                var coQuyen = await _context.NhaTro.AnyAsync(n => n.MaNhaTro == maNhaTro && n.MaChuTro == userId);
                if (!coQuyen) return Forbid();
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                var coQuyen = await _context.NguoiThue.AnyAsync(nt =>
                    nt.MaNguoiDung == userId &&
                    _context.Phong.Any(p => p.MaPhong == nt.MaPhong && p.MaNhaTro == maNhaTro));
                if (!coQuyen) return Forbid();
            }

            return await ApplyRoleFilter(_context.ChiSoDien
                    .Include(c => c.Phong)
                    .AsQueryable())
                .Where(c => c.Phong.MaNhaTro == maNhaTro)
                .OrderByDescending(c => c.NgayThangDien)
                .ToListAsync();
        }

        [HttpGet("phong/{maPhong}")]
        public async Task<ActionResult<IEnumerable<ChiSoDien>>> GetChiSoDienByPhong(int maPhong)
        {
            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(maPhong))
                return Forbid();

            if (GetCurrentRole() == VaiTroConst.NguoiDung && !await NguoiDungCoQuyenPhong(maPhong))
                return Forbid();

            return await _context.ChiSoDien
                .Where(c => c.MaPhong == maPhong)
                .OrderByDescending(c => c.NgayThangDien)
                .ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<ChiSoDien>> PostChiSoDien(ChiSoDienDtoCreate dto)
        {
            var loiValidation = await ValidateChiSoDien(dto.MaPhong, dto.SoDienCu, dto.SoDienMoi, dto.GiaDien, dto.NgayThangDien);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

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

        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutChiSoDien(int id, ChiSoDienDtoUpdate dto)
        {
            if (id != dto.MaDien)
                return BadRequest(ApiResponse<object>.Loi("Mã trên đường dẫn không khớp với mã trong dữ liệu gửi lên"));

            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(chiSoDien.MaPhong))
                return Forbid();

            var loiValidation = await ValidateChiSoDien(dto.MaPhong, dto.SoDienCu, dto.SoDienMoi, dto.GiaDien, dto.NgayThangDien, id);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            chiSoDien.MaPhong = dto.MaPhong;
            chiSoDien.SoDienCu = dto.SoDienCu;
            chiSoDien.SoDienMoi = dto.SoDienMoi;
            chiSoDien.GiaDien = dto.GiaDien;
            chiSoDien.NgayThangDien = dto.NgayThangDien;
            chiSoDien.TienDien = (dto.SoDienMoi - dto.SoDienCu) * dto.GiaDien;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteChiSoDien(int id)
        {
            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(chiSoDien.MaPhong))
                return Forbid();

            var result = await _deleteValidationService.DeleteChiSoDienAsync(id);
            return this.ToActionResult(result);

            _context.ChiSoDien.Remove(chiSoDien);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
