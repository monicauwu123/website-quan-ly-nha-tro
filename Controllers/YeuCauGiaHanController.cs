using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class YeuCauGiaHanController : ControllerBase
    {
        private const string ChoDuyet = "ChoDuyet";
        private const string DaChapNhan = "DaChapNhan";
        private const string DaTuChoi = "TuChoi";

        private readonly ApplicationDbContext _context;
        private readonly IDeleteValidationService _deleteValidationService;

        public YeuCauGiaHanController(ApplicationDbContext context, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        private IQueryable<YeuCauGiaHan> BaseQuery()
        {
            return _context.YeuCauGiaHan
                .Include(y => y.NguoiDung)
                .Include(y => y.HopDong).ThenInclude(h => h.NguoiThue)
                .Include(y => y.HopDong).ThenInclude(h => h.Phong).ThenInclude(p => p.NhaTro)
                .AsQueryable();
        }

        private static object Map(YeuCauGiaHan y)
        {
            var ngayKetThucMoi = y.NgayKetThucMoiChuTro ?? y.NgayKetThucMoiDeXuat;
            return new
            {
                y.MaYeuCauGiaHan,
                MaYeuCau = y.MaYeuCauGiaHan,
                LoaiYeuCau = "GiaHan",
                LoaiYeuCauText = "Gia hạn hợp đồng",
                y.MaHopDong,
                y.MaNguoiDung,
                y.NgayGui,
                y.NgayXuLy,
                y.TrangThai,
                TrangThaiText = y.TrangThai switch
                {
                    ChoDuyet => "Chờ duyệt",
                    DaChapNhan => "Đã chấp nhận",
                    DaTuChoi => "Từ chối",
                    _ => y.TrangThai
                },
                y.NgayKetThucCu,
                y.NgayKetThucMoiDeXuat,
                y.NgayKetThucMoiChuTro,
                NgayKetThucMoi = ngayKetThucMoi,
                y.TienCocMoi,
                y.NoiDungDieuKhoanMoi,
                y.GhiChuNguoiDung,
                y.GhiChuChuTro,
                NguoiDung = new
                {
                    y.NguoiDung.MaNguoiDung,
                    y.NguoiDung.HoTen,
                    y.NguoiDung.Email,
                    y.NguoiDung.SoDienThoai
                },
                HopDong = new
                {
                    y.HopDong.MaHopDong,
                    y.HopDong.NgayBatDau,
                    y.HopDong.NgayKetThuc,
                    y.HopDong.TienCoc,
                    y.HopDong.NoiDung
                },
                Phong = new
                {
                    y.HopDong.Phong.MaPhong,
                    y.HopDong.Phong.TenPhong,
                    NhaTro = y.HopDong.Phong.NhaTro == null ? null : new
                    {
                        y.HopDong.Phong.NhaTro.MaNhaTro,
                        y.HopDong.Phong.NhaTro.TenNhaTro,
                        y.HopDong.Phong.NhaTro.DiaChi
                    }
                },
                NguoiThue = new
                {
                    y.HopDong.NguoiThue.MaNguoiThue,
                    y.HopDong.NguoiThue.HoTen,
                    y.HopDong.NguoiThue.SDT,
                    y.HopDong.NguoiThue.Email
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetYeuCauGiaHan()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            var query = BaseQuery();

            if (role == VaiTroConst.NguoiDung)
                query = query.Where(y => y.MaNguoiDung == userId);
            else if (role == VaiTroConst.ChuTro)
                query = query.Where(y => y.HopDong.Phong.NhaTro.MaChuTro == userId);

            var data = await query.OrderByDescending(y => y.NgayGui).ToListAsync();
            return Ok(ApiResponse<List<object>>.Ok(data.Select(Map).ToList()));
        }

        [HttpPost]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> TaoYeuCauGiaHan([FromBody] TaoYeuCauGiaHanDto dto)
        {
            var userId = GetCurrentUserId();
            var hopDong = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHopDong == dto.MaHopDong);

            if (hopDong == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy hợp đồng"));

            if (hopDong.NguoiThue.MaNguoiDung != userId)
                return Forbid();

            var mocSoSanh = hopDong.NgayKetThuc ?? hopDong.NgayBatDau;
            if (dto.NgayKetThucMoiDeXuat <= mocSoSanh)
                return BadRequest(ApiResponse<object>.Loi("Ngày kết thúc mới phải lớn hơn ngày kết thúc hiện tại của hợp đồng"));

            var daCoYeuCauChoDuyet = await _context.YeuCauGiaHan.AnyAsync(y =>
                y.MaNguoiDung == userId && y.MaHopDong == dto.MaHopDong && y.TrangThai == ChoDuyet);

            if (daCoYeuCauChoDuyet)
                return BadRequest(ApiResponse<object>.Loi("Bạn đã gửi yêu cầu gia hạn hợp đồng này và đang chờ chủ trọ xử lý"));

            var yeuCau = new YeuCauGiaHan
            {
                MaHopDong = dto.MaHopDong,
                MaNguoiDung = userId,
                NgayKetThucCu = hopDong.NgayKetThuc,
                NgayKetThucMoiDeXuat = dto.NgayKetThucMoiDeXuat,
                GhiChuNguoiDung = dto.GhiChuNguoiDung,
                TrangThai = ChoDuyet,
                NgayGui = DateTime.Now
            };

            _context.YeuCauGiaHan.Add(yeuCau);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { yeuCau.MaYeuCauGiaHan }, "Gửi yêu cầu gia hạn thành công"));
        }

        [HttpPost("{id}/chap-nhan")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> ChapNhan(int id, [FromBody] DuyetYeuCauGiaHanDto dto)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var yeuCau = await BaseQuery().FirstOrDefaultAsync(y => y.MaYeuCauGiaHan == id);
            if (yeuCau == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu gia hạn"));

            if (role == VaiTroConst.ChuTro && yeuCau.HopDong.Phong.NhaTro.MaChuTro != userId)
                return Forbid();

            if (yeuCau.TrangThai != ChoDuyet)
                return BadRequest(ApiResponse<object>.Loi("Yêu cầu này đã được xử lý"));

            var ngayMoi = dto.NgayKetThucMoi ?? yeuCau.NgayKetThucMoiDeXuat;
            var mocSoSanh = yeuCau.HopDong.NgayKetThuc ?? yeuCau.HopDong.NgayBatDau;
            if (ngayMoi <= mocSoSanh)
                return BadRequest(ApiResponse<object>.Loi("Ngày kết thúc mới phải lớn hơn ngày kết thúc hiện tại"));

            yeuCau.HopDong.NgayKetThuc = ngayMoi;

            if (dto.TienCocMoi.HasValue)
                yeuCau.HopDong.TienCoc = dto.TienCocMoi.Value;

            if (!string.IsNullOrWhiteSpace(dto.NoiDungDieuKhoanMoi))
                yeuCau.HopDong.NoiDung = dto.NoiDungDieuKhoanMoi;

            yeuCau.TrangThai = DaChapNhan;
            yeuCau.NgayKetThucMoiChuTro = ngayMoi;
            yeuCau.TienCocMoi = dto.TienCocMoi;
            yeuCau.NoiDungDieuKhoanMoi = dto.NoiDungDieuKhoanMoi;
            yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
            yeuCau.NgayXuLy = DateTime.Now;

            // Đảm bảo phòng ở trạng thái "Đã thuê" sau khi gia hạn thành công.
            var phong = await _context.Phong.FindAsync(yeuCau.HopDong.MaPhong);
            if (phong != null)
            {
                var trangThaiDaThue = await _context.TrangThai
                    .FirstOrDefaultAsync(t => t.TenTrangThai.Contains("thuê") || t.TenTrangThai.Contains("thue"));
                if (trangThaiDaThue != null)
                    phong.MaTrangThai = trangThaiDaThue.MaTrangThai;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                yeuCau.MaYeuCauGiaHan,
                yeuCau.MaHopDong,
                NgayKetThucMoi = ngayMoi
            }, "Đã chấp nhận yêu cầu gia hạn và cập nhật hợp đồng"));
        }

        [HttpPost("{id}/tu-choi")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> TuChoi(int id, [FromBody] TuChoiYeuCauThueDto dto)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var yeuCau = await BaseQuery().FirstOrDefaultAsync(y => y.MaYeuCauGiaHan == id);
            if (yeuCau == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu gia hạn"));

            if (role == VaiTroConst.ChuTro && yeuCau.HopDong.Phong.NhaTro.MaChuTro != userId)
                return Forbid();

            if (yeuCau.TrangThai != ChoDuyet)
                return BadRequest(ApiResponse<object>.Loi("Chỉ có thể từ chối yêu cầu đang chờ duyệt"));

            yeuCau.TrangThai = DaTuChoi;
            yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
            yeuCau.NgayXuLy = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null!, "Đã từ chối yêu cầu gia hạn"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> HuyYeuCauGiaHan(int id)
        {
            var userId = GetCurrentUserId();
            var yeuCau = await _context.YeuCauGiaHan.FirstOrDefaultAsync(y => y.MaYeuCauGiaHan == id);
            if (yeuCau == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu gia hạn"));

            if (yeuCau.MaNguoiDung != userId)
                return Forbid();

            // Chỉ hủy được khi đang chờ duyệt → Xóa cứng
            var result = await _deleteValidationService.DeleteYeuCauGiaHanAsync(id);
            return this.ToActionResult(result);

            if (yeuCau.TrangThai == ChoDuyet)
            {
                _context.YeuCauGiaHan.Remove(yeuCau);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null!, "Đã hủy yêu cầu gia hạn"));
            }

            // Đã được xử lý (chấp nhận/từ chối) → Giữ lịch sử, không xóa
            return BadRequest(ApiResponse<object>.Loi(
                $"Yêu cầu gia hạn đã được xử lý (trạng thái: {yeuCau.TrangThai}). " +
                "Không thể xóa để giữ lịch sử dữ liệu. Chỉ hủy được khi yêu cầu đang chờ duyệt."));
        }
    }
}
