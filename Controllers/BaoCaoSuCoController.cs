using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BaoCaoSuCoController : ControllerBase
    {
        private const string Moi = "Moi";
        private const string DangXuLy = "DangXuLy";
        private const string DaXuLy = "DaXuLy";
        private const string Huy = "Huy";

        private readonly ApplicationDbContext _context;
        private readonly INotificationEmailService _notificationEmailService;
        private readonly IDeleteValidationService _deleteValidationService;

        public BaoCaoSuCoController(ApplicationDbContext context, INotificationEmailService notificationEmailService, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _notificationEmailService = notificationEmailService;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        private IQueryable<BaoCaoSuCo> BaseQuery()
        {
            return _context.BaoCaoSuCo
                .Include(b => b.NguoiDung)
                .Include(b => b.Phong).ThenInclude(p => p.NhaTro)
                .AsQueryable();
        }

        private static string TrangThaiText(string? trangThai)
        {
            return trangThai switch
            {
                Moi => "Mới gửi",
                DangXuLy => "Đang xử lý",
                DaXuLy => "Đã xử lý",
                Huy => "Đã hủy",
                _ => trangThai ?? "---"
            };
        }

        private static object MapBaoCao(BaoCaoSuCo b)
        {
            return new
            {
                b.MaBaoCao,
                b.MaNguoiDung,
                b.MaPhong,
                b.TieuDe,
                b.NoiDung,
                b.MucDo,
                b.TrangThai,
                TrangThaiText = TrangThaiText(b.TrangThai),
                b.NgayGui,
                b.NgayXuLy,
                b.PhanHoiChuTro,
                NguoiDung = b.NguoiDung == null ? null : new
                {
                    b.NguoiDung.MaNguoiDung,
                    b.NguoiDung.HoTen,
                    b.NguoiDung.Email,
                    b.NguoiDung.SoDienThoai
                },
                Phong = b.Phong == null ? null : new
                {
                    b.Phong.MaPhong,
                    b.Phong.TenPhong,
                    b.Phong.DiaChiPhong,
                    NhaTro = b.Phong.NhaTro == null ? null : new
                    {
                        b.Phong.NhaTro.MaNhaTro,
                        b.Phong.NhaTro.TenNhaTro,
                        b.Phong.NhaTro.DiaChi
                    }
                }
            };
        }

        private async Task<bool> NguoiDungDangThuePhong(int maNguoiDung, int maPhong)
        {
            var today = DateTime.Today;

            return await _context.HopDong
                .Include(h => h.NguoiThue)
                .AnyAsync(h =>
                    h.MaPhong == maPhong &&
                    h.NguoiThue.MaNguoiDung == maNguoiDung &&
                    h.NgayBatDau.Date <= today &&
                    (h.NgayKetThuc == null || h.NgayKetThuc.Value.Date >= today));
        }

        private async Task<bool> PhongThuocChuTro(int maPhong, int maChuTro)
        {
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == maChuTro);
        }

        // GET: api/BaoCaoSuCo
        [HttpGet]
        public async Task<IActionResult> GetBaoCaoSuCo()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var query = BaseQuery();

                if (role == VaiTroConst.ChuTro)
                    query = query.Where(b => b.Phong.NhaTro.MaChuTro == userId);
                else if (role == VaiTroConst.NguoiDung)
                    query = query.Where(b => b.MaNguoiDung == userId);

                var data = await query
                    .OrderByDescending(b => b.NgayGui)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.Ok(data.Select(MapBaoCao).ToList()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/BaoCaoSuCo/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBaoCaoSuCo(int id)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var baoCao = await BaseQuery().FirstOrDefaultAsync(b => b.MaBaoCao == id);
                if (baoCao == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy báo cáo sự cố"));

                if (role == VaiTroConst.NguoiDung && baoCao.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro && baoCao.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                return Ok(ApiResponse<object>.Ok(MapBaoCao(baoCao)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/BaoCaoSuCo/TaoMoi
        [HttpGet("TaoMoi")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> GetTaoMoi()
        {
            try
            {
                var userId = GetCurrentUserId();
                var today = DateTime.Today;

                var phongDangThueRaw = await _context.HopDong
                    .Include(h => h.NguoiThue)
                    .Include(h => h.Phong).ThenInclude(p => p.NhaTro)
                    .Where(h =>
                        h.NguoiThue.MaNguoiDung == userId &&
                        h.NgayBatDau.Date <= today &&
                        (h.NgayKetThuc == null || h.NgayKetThuc.Value.Date >= today))
                    .Select(h => new
                    {
                        h.Phong.MaPhong,
                        h.Phong.TenPhong,
                        h.Phong.DiaChiPhong,
                        h.Phong.GiaPhong,
                        NhaTro = h.Phong.NhaTro == null ? null : new
                        {
                            h.Phong.NhaTro.MaNhaTro,
                            h.Phong.NhaTro.TenNhaTro,
                            h.Phong.NhaTro.DiaChi
                        }
                    })
                    .ToListAsync();

                var phongDangThue = phongDangThueRaw
                    .GroupBy(p => p.MaPhong)
                    .Select(g => g.First())
                    .ToList();

                return Ok(ApiResponse<object>.Ok(new
                {
                    phongDangThue,
                    mucDo = new[] { "Bình thường", "Gấp", "Rất gấp" }
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/BaoCaoSuCo
        [HttpPost]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> PostBaoCaoSuCo([FromBody] TaoBaoCaoSuCoDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .FirstOrDefaultAsync(p => p.MaPhong == dto.MaPhong);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Phòng không tồn tại"));

                var dangThuePhong = await NguoiDungDangThuePhong(userId, dto.MaPhong);
                if (!dangThuePhong)
                    return BadRequest(ApiResponse<object>.Loi("Bạn chỉ có thể báo cáo sự cố cho phòng đang thuê"));

                var mucDo = string.IsNullOrWhiteSpace(dto.MucDo) ? "Bình thường" : dto.MucDo.Trim();
                var mucDoHopLe = new[] { "Bình thường", "Gấp", "Rất gấp" };
                if (!mucDoHopLe.Contains(mucDo))
                    return BadRequest(ApiResponse<object>.Loi("Mức độ sự cố không hợp lệ"));

                var baoCao = new BaoCaoSuCo
                {
                    MaNguoiDung = userId,
                    MaPhong = dto.MaPhong,
                    TieuDe = dto.TieuDe.Trim(),
                    NoiDung = dto.NoiDung.Trim(),
                    MucDo = mucDo,
                    TrangThai = Moi,
                    NgayGui = DateTime.Now
                };

                _context.BaoCaoSuCo.Add(baoCao);
                await _context.SaveChangesAsync();
                await _notificationEmailService.GuiEmailBaoCaoSuCoMoiAsync(baoCao.MaBaoCao);

                return CreatedAtAction(nameof(GetBaoCaoSuCo), new { id = baoCao.MaBaoCao },
                    ApiResponse<object>.Ok(new { baoCao.MaBaoCao }, "Gửi báo cáo sự cố thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/BaoCaoSuCo/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro},{VaiTroConst.NguoiDung}")]
        public async Task<IActionResult> PutBaoCaoSuCo(int id, [FromBody] CapNhatBaoCaoSuCoDto dto)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var baoCao = await _context.BaoCaoSuCo
                    .Include(b => b.Phong).ThenInclude(p => p.NhaTro)
                    .FirstOrDefaultAsync(b => b.MaBaoCao == id);

                if (baoCao == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy báo cáo sự cố"));

                if (role == VaiTroConst.NguoiDung)
                {
                    if (baoCao.MaNguoiDung != userId)
                        return Forbid();

                    if (baoCao.TrangThai != Moi)
                        return BadRequest(ApiResponse<object>.Loi("Chỉ có thể sửa báo cáo khi chủ trọ chưa tiếp nhận xử lý"));

                    if (dto.MaPhong.HasValue && dto.MaPhong.Value != baoCao.MaPhong)
                    {
                        var dangThuePhong = await NguoiDungDangThuePhong(userId, dto.MaPhong.Value);
                        if (!dangThuePhong)
                            return BadRequest(ApiResponse<object>.Loi("Bạn chỉ có thể báo cáo sự cố cho phòng đang thuê"));

                        baoCao.MaPhong = dto.MaPhong.Value;
                    }

                    if (string.IsNullOrWhiteSpace(dto.TieuDe))
                        return BadRequest(ApiResponse<object>.Loi("Tiêu đề không được để trống"));

                    if (string.IsNullOrWhiteSpace(dto.NoiDung))
                        return BadRequest(ApiResponse<object>.Loi("Nội dung sự cố không được để trống"));

                    var mucDo = string.IsNullOrWhiteSpace(dto.MucDo) ? "Bình thường" : dto.MucDo.Trim();
                    var mucDoHopLe = new[] { "Bình thường", "Gấp", "Rất gấp" };
                    if (!mucDoHopLe.Contains(mucDo))
                        return BadRequest(ApiResponse<object>.Loi("Mức độ sự cố không hợp lệ"));

                    baoCao.TieuDe = dto.TieuDe.Trim();
                    baoCao.NoiDung = dto.NoiDung.Trim();
                    baoCao.MucDo = mucDo;

                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(MapBaoCao(baoCao), "Cập nhật báo cáo sự cố thành công"));
                }

                if (role == VaiTroConst.ChuTro && baoCao.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                var trangThai = dto.TrangThai?.Trim();
                var trangThaiHopLe = new[] { Moi, DangXuLy, DaXuLy };
                if (string.IsNullOrWhiteSpace(trangThai) || !trangThaiHopLe.Contains(trangThai))
                    return BadRequest(ApiResponse<object>.Loi("Trạng thái báo cáo không hợp lệ"));

                baoCao.TrangThai = trangThai;
                baoCao.PhanHoiChuTro = dto.PhanHoiChuTro;
                baoCao.NgayXuLy = trangThai == Moi ? null : DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(MapBaoCao(baoCao), "Cập nhật báo cáo sự cố thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/BaoCaoSuCo/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBaoCaoSuCo(int id)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var baoCao = await _context.BaoCaoSuCo
                    .Include(b => b.Phong).ThenInclude(p => p.NhaTro)
                    .FirstOrDefaultAsync(b => b.MaBaoCao == id);

                if (baoCao == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy báo cáo sự cố"));

                if (role == VaiTroConst.NguoiDung)
                {
                    if (baoCao.MaNguoiDung != userId)
                        return Forbid();

                    // Chỉ hủy được khi mới gửi (chủ trọ chưa xử lý)
                    if (baoCao.TrangThai == Moi)
                    {
                        baoCao.TrangThai = Huy;
                        baoCao.NgayXuLy = DateTime.Now;
                        await _context.SaveChangesAsync();
                        return Ok(ApiResponse<object>.Ok(null!, "Đã hủy báo cáo sự cố"));
                    }

                    // Đang xử lý / đã xử lý → giữ lịch sử
                    var result = await _deleteValidationService.DeleteBaoCaoSuCoAsync(id);
                    return this.ToActionResult(result);

                    return BadRequest(ApiResponse<object>.Loi(
                        $"Báo cáo đang ở trạng thái \"{TrangThaiText(baoCao.TrangThai)}\", " +
                        "không thể hủy để giữ lịch sử xử lý. Chỉ hủy được khi chủ trọ chưa tiếp nhận."));
                }

                if (role == VaiTroConst.ChuTro)
                {
                    if (baoCao.Phong.NhaTro.MaChuTro != userId)
                        return Forbid();

                    // ChuTro: Giữ lịch sử, không xóa cứng
                    return BadRequest(ApiResponse<object>.Loi(
                        "Chủ trọ không thể xóa báo cáo sự cố để giữ lịch sử. " +
                        "Hãy cập nhật trạng thái xử lý thay vì xóa."));
                }

                // Admin: Xóa cứng
                var adminResult = await _deleteValidationService.DeleteBaoCaoSuCoAsync(id);
                return this.ToActionResult(adminResult);

                _context.BaoCaoSuCo.Remove(baoCao);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null!, "Đã xóa báo cáo sự cố thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
