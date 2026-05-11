using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DangKyDichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDeleteValidationService _deleteValidationService;

        public DangKyDichVuController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private const string TrangThaiDangSuDung = "DangSuDung";
        private const string TrangThaiDaHuy = "DaHuy";
        private const string TrangThaiHetHan = "HetHan";

        private static string LayTenTrangThai(string? trangThai)
            => trangThai switch
            {
                TrangThaiDaHuy => "Đã hủy",
                TrangThaiHetHan => "Hết hạn kỳ thuê",
                _ => "Đang sử dụng"
            };

        private async Task<bool> NguoiDungDangThuePhong(int maNguoiDung, int maPhong)
        {
            return await _context.HopDong
                .Include(h => h.NguoiThue)
                .AnyAsync(h => h.MaPhong == maPhong
                    && h.NguoiThue.MaNguoiDung == maNguoiDung
                    && (h.NgayKetThuc == null || h.NgayKetThuc > DateTime.Now));
        }

        private async Task<int?> LayMaNguoiThueDangThuePhong(int maNguoiDung, int maPhong)
        {
            return await _context.HopDong
                .Include(h => h.NguoiThue)
                .Where(h => h.MaPhong == maPhong
                    && h.NguoiThue.MaNguoiDung == maNguoiDung
                    && (h.NgayKetThuc == null || h.NgayKetThuc > DateTime.Now))
                .OrderByDescending(h => h.NgayBatDau)
                .Select(h => (int?)h.MaNguoiThue)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> ChuTroCoQuyenPhong(int maPhong)
        {
            var userId = GetCurrentUserId();
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == userId);
        }

        private IQueryable<DangKyDichVu> BuildQueryTheoQuyen()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var query = _context.DangKyDichVu
                .Include(dk => dk.NguoiDung)
                .Include(dk => dk.DichVu)
                .Include(dk => dk.Phong)
                    .ThenInclude(p => p.NhaTro)
                .AsQueryable();

            if (role == VaiTroConst.NguoiDung)
            {
                query = query.Where(dk => dk.MaNguoiDung == userId);
            }
            else if (role == VaiTroConst.ChuTro)
            {
                query = query.Where(dk => dk.Phong.NhaTro.MaChuTro == userId);
            }

            return query;
        }

        private static DangKyDichVuDto ToDto(DangKyDichVu dk)
        {
            return new DangKyDichVuDto
            {
                MaDangKyDichVu = dk.MaDangKyDichVu,
                MaNguoiDung = dk.MaNguoiDung,
                MaPhong = dk.MaPhong,
                TenPhong = dk.Phong?.TenPhong ?? $"Phòng #{dk.MaPhong}",
                TenNhaTro = dk.Phong?.NhaTro?.TenNhaTro ?? string.Empty,
                MaDichVu = dk.MaDichVu,
                TenDichVu = dk.DichVu?.TenDichVu ?? $"Dịch vụ #{dk.MaDichVu}",
                TienDichVu = dk.DichVu == null ? 0 : (decimal)dk.DichVu.Tiendichvu,
                NgayDangKy = dk.NgayDangKy,
                NgayHuy = dk.NgayHuy,
                NgayHetHan = dk.NgayHetHan,
                KyDangKy = dk.KyDangKy,
                TrangThai = dk.TrangThai,
                TenTrangThai = LayTenTrangThai(dk.TrangThai),
                GhiChu = dk.GhiChu,
                TenNguoiDung = dk.NguoiDung?.HoTen,
                SoDienThoai = dk.NguoiDung?.SoDienThoai,
                Email = dk.NguoiDung?.Email
            };
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DangKyDichVuDto>>> GetAll()
        {
            var data = await BuildQueryTheoQuyen()
                .OrderByDescending(dk => dk.TrangThai == TrangThaiDangSuDung)
                .ThenByDescending(dk => dk.NgayDangKy)
                .ToListAsync();

            return Ok(data.Select(ToDto).ToList());
        }

        [HttpGet("PhongDangThue")]
        [Authorize(Roles = "NguoiDung")]
        public async Task<IActionResult> GetPhongDangThue()
        {
            var userId = GetCurrentUserId();

            var phong = await _context.HopDong
                .Include(h => h.Phong)
                    .ThenInclude(p => p.NhaTro)
                .Include(h => h.NguoiThue)
                .Where(h => h.NguoiThue.MaNguoiDung == userId
                    && (h.NgayKetThuc == null || h.NgayKetThuc > DateTime.Now))
                .Select(h => new
                {
                    h.Phong.MaPhong,
                    h.Phong.TenPhong,
                    h.Phong.GiaPhong,
                    TenNhaTro = h.Phong.NhaTro.TenNhaTro,
                    h.MaNguoiThue
                })
                .Distinct()
                .OrderBy(x => x.TenNhaTro)
                .ThenBy(x => x.TenPhong)
                .ToListAsync();

            return Ok(phong);
        }

        [HttpGet("DichVuTheoPhong/{maPhong}")]
        [Authorize(Roles = "NguoiDung")]
        public async Task<IActionResult> GetDichVuTheoPhong(int maPhong)
        {
            var userId = GetCurrentUserId();

            if (!await NguoiDungDangThuePhong(userId, maPhong))
                return Forbid();

            var phong = await _context.Phong
                .Include(p => p.NhaTro)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (phong == null)
                return NotFound(ApiResponse<object>.Loi("Phòng không tồn tại"));

            var activeIds = await _context.DangKyDichVu
                .Where(dk => dk.MaNguoiDung == userId
                    && dk.MaPhong == maPhong
                    && dk.TrangThai == TrangThaiDangSuDung)
                .Select(dk => dk.MaDichVu)
                .ToListAsync();

            var dichVu = await _context.DichVu
                .Where(dv => dv.MaNhaTro == phong.MaNhaTro)
                .Where(dv => dv.LoaiDichVu == "TinhPhi")
                .OrderBy(dv => dv.TenDichVu)
                .Select(dv => new
                {
                    dv.MaDichVu,
                    dv.TenDichVu,
                    TienDichVu = (decimal)dv.Tiendichvu,
                    DaDangKy = activeIds.Contains(dv.MaDichVu)
                })
                .ToListAsync();

            return Ok(dichVu);
        }

        [HttpPost]
        [Authorize(Roles = "NguoiDung")]
        public async Task<IActionResult> Create([FromBody] DangKyDichVuCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            if (!await NguoiDungDangThuePhong(userId, dto.MaPhong))
                return BadRequest(ApiResponse<object>.Loi("Bạn chỉ được đăng ký dịch vụ cho phòng đang thuê"));

            var phong = await _context.Phong
                .Include(p => p.NhaTro)
                .FirstOrDefaultAsync(p => p.MaPhong == dto.MaPhong);

            if (phong == null)
                return BadRequest(ApiResponse<object>.Loi("Phòng không tồn tại"));

            var dichVu = await _context.DichVu.FirstOrDefaultAsync(dv =>
                dv.MaDichVu == dto.MaDichVu
                && dv.MaNhaTro == phong.MaNhaTro
                && dv.LoaiDichVu == "TinhPhi");

            if (dichVu == null)
                return BadRequest(ApiResponse<object>.Loi("Dịch vụ không tồn tại hoặc không thuộc nhà trọ của phòng này"));

            var daDangKy = await _context.DangKyDichVu.AnyAsync(dk =>
                dk.MaNguoiDung == userId
                && dk.MaPhong == dto.MaPhong
                && dk.MaDichVu == dto.MaDichVu
                && dk.TrangThai == TrangThaiDangSuDung);

            if (daDangKy)
                return BadRequest(ApiResponse<object>.Loi("Bạn đã đăng ký dịch vụ này cho phòng đã chọn"));

            var maNguoiThue = await LayMaNguoiThueDangThuePhong(userId, dto.MaPhong);

            var dangKy = new DangKyDichVu
            {
                MaNguoiDung = userId,
                MaNguoiThue = maNguoiThue,
                MaPhong = dto.MaPhong,
                MaDichVu = dto.MaDichVu,
                NgayDangKy = DateTime.Now,
                KyDangKy = DateTime.Now.ToString("yyyy-MM"),
                TrangThai = TrangThaiDangSuDung,
                GhiChu = dto.GhiChu
            };

            _context.DangKyDichVu.Add(dangKy);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { dangKy.MaDangKyDichVu }, "Đăng ký dịch vụ thành công"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ChuTro,NguoiDung")]
        public async Task<IActionResult> Huy(int id)
        {
            var dangKy = await _context.DangKyDichVu
                .Include(dk => dk.Phong)
                    .ThenInclude(p => p.NhaTro)
                .FirstOrDefaultAsync(dk => dk.MaDangKyDichVu == id);

            if (dangKy == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy đăng ký dịch vụ"));

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.NguoiDung && dangKy.MaNguoiDung != userId)
                return Forbid();

            if (role == VaiTroConst.ChuTro && dangKy.Phong.NhaTro.MaChuTro != userId)
                return Forbid();

            if (dangKy.TrangThai == TrangThaiDaHuy)
                return BadRequest(ApiResponse<object>.Loi("Đăng ký dịch vụ này đã được hủy trước đó"));

            if (dangKy.TrangThai == TrangThaiHetHan)
                return BadRequest(ApiResponse<object>.Loi("Đăng ký dịch vụ này đã hết hạn theo kỳ thuê"));

            var result = await _deleteValidationService.DeleteDangKyDichVuAsync(id);
            return this.ToActionResult(result);

            dangKy.TrangThai = TrangThaiDaHuy;
            dangKy.NgayHuy = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null, "Đã hủy đăng ký dịch vụ"));
        }
    }
}
