using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;
using DoAnSE104.Services.Interfaces;
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
        private readonly IDeleteValidationService _deleteValidationService;

        public ChiSoNuocController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
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

        private IQueryable<ChiSoNuoc> ApplyRoleFilter(IQueryable<ChiSoNuoc> query)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
                query = query.Where(c => c.Phong.NhaTro.MaChuTro == userId);
            else if (role == VaiTroConst.NguoiDung)
                query = query.Where(c => _context.NguoiThue.Any(nt => nt.MaNguoiDung == userId && nt.MaPhong == c.MaPhong));

            return query;
        }

        private async Task<string?> ValidateChiSoNuoc(int maPhong, int soNuocCu, int soNuocMoi, decimal giaNuoc, DateTime ngayThangNuoc, int? maNuocBoQua = null)
        {
            if (soNuocCu < 0)
                return "Chỉ số nước cũ không được âm";

            if (soNuocMoi < 0)
                return "Chỉ số nước mới không được âm";

            if (giaNuoc < 0)
                return "Giá nước không được âm";

            if (soNuocMoi < soNuocCu)
                return "Chỉ số nước mới phải lớn hơn hoặc bằng chỉ số nước cũ";

            var phongTonTai = await _context.Phong.AnyAsync(p => p.MaPhong == maPhong);
            if (!phongTonTai)
                return "Phòng không tồn tại";

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(maPhong))
                return "Bạn không có quyền ghi chỉ số nước cho phòng này";

            var trungThang = await _context.ChiSoNuoc.AnyAsync(c =>
                c.MaPhong == maPhong &&
                c.NgayThangNuoc.Month == ngayThangNuoc.Month &&
                c.NgayThangNuoc.Year == ngayThangNuoc.Year &&
                (!maNuocBoQua.HasValue || c.MaNuoc != maNuocBoQua.Value));

            if (trungThang)
                return "Đã tồn tại chỉ số nước của phòng này trong tháng đã chọn";

            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChiSoNuoc>>> GetChiSoNuoc()
        {
            return await ApplyRoleFilter(_context.ChiSoNuoc.Include(c => c.Phong).AsQueryable())
                .OrderByDescending(c => c.NgayThangNuoc)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChiSoNuoc>> GetChiSoNuoc(int id)
        {
            var chiSoNuoc = await ApplyRoleFilter(_context.ChiSoNuoc.Include(c => c.Phong).AsQueryable())
                .FirstOrDefaultAsync(c => c.MaNuoc == id);

            if (chiSoNuoc == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            return chiSoNuoc;
        }


        [HttpGet("nha-tro/{maNhaTro}")]
        public async Task<ActionResult<IEnumerable<ChiSoNuoc>>> GetChiSoNuocByNhaTro(int maNhaTro)
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

            return await ApplyRoleFilter(_context.ChiSoNuoc
                    .Include(c => c.Phong)
                    .AsQueryable())
                .Where(c => c.Phong.MaNhaTro == maNhaTro)
                .OrderByDescending(c => c.NgayThangNuoc)
                .ToListAsync();
        }

        [HttpGet("phong/{maPhong}")]
        public async Task<ActionResult<IEnumerable<ChiSoNuoc>>> GetChiSoNuocByPhong(int maPhong)
        {
            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(maPhong))
                return Forbid();

            if (GetCurrentRole() == VaiTroConst.NguoiDung && !await NguoiDungCoQuyenPhong(maPhong))
                return Forbid();

            return await _context.ChiSoNuoc
                .Where(c => c.MaPhong == maPhong)
                .OrderByDescending(c => c.NgayThangNuoc)
                .ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<ChiSoNuoc>> PostChiSoNuoc(ChiSoNuocDtoCreate dto)
        {
            var loiValidation = await ValidateChiSoNuoc(dto.MaPhong, dto.SoNuocCu, dto.SoNuocMoi, dto.GiaNuoc, dto.NgayThangNuoc);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

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
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutChiSoNuoc(int id, ChiSoNuocDtoUpdate dto)
        {
            if (id != dto.MaNuoc)
                return BadRequest(ApiResponse<object>.Loi("Mã trên đường dẫn không khớp với mã trong dữ liệu gửi lên"));

            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(chiSoNuoc.MaPhong))
                return Forbid();

            var loiValidation = await ValidateChiSoNuoc(dto.MaPhong, dto.SoNuocCu, dto.SoNuocMoi, dto.GiaNuoc, dto.NgayThangNuoc, id);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

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
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteChiSoNuoc(int id)
        {
            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(chiSoNuoc.MaPhong))
                return Forbid();

            var result = await _deleteValidationService.DeleteChiSoNuocAsync(id);
            return this.ToActionResult(result);

            _context.ChiSoNuoc.Remove(chiSoNuoc);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
