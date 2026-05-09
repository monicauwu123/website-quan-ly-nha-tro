using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using DoAnSE104.Services;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class YeuCauThueController : ControllerBase
    {
        private const string ChoDuyet = "ChoDuyet";
        private const string DaChapNhan = "DaChapNhan";
        private const string TuChoi = "TuChoi";
        private const string DaLapHopDong = "DaLapHopDong";

        private readonly ApplicationDbContext _context;
        private readonly INotificationEmailService _notificationEmailService;

        public YeuCauThueController(ApplicationDbContext context, INotificationEmailService notificationEmailService)
        {
            _context = context;
            _notificationEmailService = notificationEmailService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        private async Task<bool> PhongThuocChuTro(int maPhong, int maChuTro)
        {
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == maChuTro);
        }

        private static DateTime TinhNgayKetThucTheoSoThang(DateTime ngayBatDau, int soThang)
            => ngayBatDau.Date.AddMonths(Math.Max(soThang, 1)).AddDays(-1);

        private IQueryable<YeuCauThue> BaseQuery()
        {
            return _context.YeuCauThue
                .Include(y => y.NguoiDung)
                .Include(y => y.Phong).ThenInclude(p => p.NhaTro)
                .Include(y => y.NguoiThue)
                .Include(y => y.HopDong)
                .AsQueryable();
        }

        private static object MapYeuCau(YeuCauThue y)
        {
            return new
            {
                y.MaYeuCau,
                y.MaNguoiDung,
                y.MaPhong,
                y.MaNguoiThue,
                y.MaHopDong,
                y.NgayGui,
                y.NgayXuLy,
                y.TrangThai,
                TrangThaiText = y.TrangThai switch
                {
                    ChoDuyet => "Chá» duyá»‡t",
                    DaChapNhan => "ÄÃ£ cháº¥p nháº­n",
                    DaLapHopDong => "ÄÃ£ láº­p há»£p Ä‘á»“ng",
                    TuChoi => "Tá»« chá»‘i",
                    _ => y.TrangThai
                },
                y.GhiChuNguoiDung,
                y.GhiChuChuTro,
                y.SoThangMuonThue,
                y.NgayBatDauMongMuon,
                NguoiDung = new
                {
                    y.NguoiDung.MaNguoiDung,
                    y.NguoiDung.HoTen,
                    y.NguoiDung.Email,
                    y.NguoiDung.SoDienThoai
                },
                Phong = new
                {
                    y.Phong.MaPhong,
                    y.Phong.TenPhong,
                    y.Phong.GiaPhong,
                    y.Phong.DiaChiPhong,
                    NhaTro = y.Phong.NhaTro == null ? null : new
                    {
                        y.Phong.NhaTro.MaNhaTro,
                        y.Phong.NhaTro.TenNhaTro,
                        y.Phong.NhaTro.DiaChi
                    }
                }
            };
        }

        // GET: api/YeuCauThue
        [HttpGet]
        public async Task<IActionResult> GetYeuCauThue()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var query = BaseQuery();

                if (role == VaiTroConst.ChuTro)
                {
                    query = query.Where(y => y.Phong.NhaTro.MaChuTro == userId);
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    query = query.Where(y => y.MaNguoiDung == userId);
                }

                var data = await query
                    .OrderByDescending(y => y.NgayGui)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.Ok(data.Select(MapYeuCau).ToList()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/YeuCauThue/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetYeuCauThue(int id)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var yeuCau = await BaseQuery().FirstOrDefaultAsync(y => y.MaYeuCau == id);
                if (yeuCau == null)
                    return NotFound(ApiResponse<object>.Loi("KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u thuÃª"));

                if (role == VaiTroConst.NguoiDung && yeuCau.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                return Ok(ApiResponse<object>.Ok(MapYeuCau(yeuCau)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/YeuCauThue
        [HttpPost]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> PostYeuCauThue([FromBody] TaoYeuCauThueDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (dto.SoThangMuonThue < 1 || dto.SoThangMuonThue > 60)
                    return BadRequest(ApiResponse<object>.Loi("Sá»‘ thÃ¡ng muá»‘n thuÃª pháº£i tá»« 1 Ä‘áº¿n 60"));

                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .FirstOrDefaultAsync(p => p.MaPhong == dto.MaPhong);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("PhÃ²ng khÃ´ng tá»“n táº¡i"));

                var daCoHopDongHieuLuc = await _context.HopDong
                    .AnyAsync(h => h.Phong.MaPhong == dto.MaPhong && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));
                if (daCoHopDongHieuLuc)
                    return BadRequest(ApiResponse<object>.Loi("PhÃ²ng nÃ y Ä‘Ã£ cÃ³ há»£p Ä‘á»“ng hiá»‡u lá»±c"));

                var daCoYeuCauChoDuyet = await _context.YeuCauThue.AnyAsync(y =>
                    y.MaNguoiDung == userId &&
                    y.MaPhong == dto.MaPhong &&
                    y.TrangThai == ChoDuyet);

                if (daCoYeuCauChoDuyet)
                    return BadRequest(ApiResponse<object>.Loi("Báº¡n Ä‘Ã£ gá»­i yÃªu cáº§u thuÃª phÃ²ng nÃ y vÃ  Ä‘ang chá» chá»§ trá» duyá»‡t"));

                var yeuCau = new YeuCauThue
                {
                    MaNguoiDung = userId,
                    MaPhong = dto.MaPhong,
                    GhiChuNguoiDung = dto.GhiChuNguoiDung,
                    SoThangMuonThue = dto.SoThangMuonThue,
                    NgayBatDauMongMuon = dto.NgayBatDauMongMuon,
                    TrangThai = ChoDuyet,
                    NgayGui = DateTime.Now
                };

                _context.YeuCauThue.Add(yeuCau);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetYeuCauThue), new { id = yeuCau.MaYeuCau },
                    ApiResponse<YeuCauThue>.Ok(yeuCau, "Gá»­i yÃªu cáº§u thuÃª thÃ nh cÃ´ng"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/YeuCauThue/5/chap-nhan
        [HttpPost("{id}/chap-nhan")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> ChapNhanYeuCauThue(int id, [FromBody] ChapNhanYeuCauThueDto dto)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var strategy = _context.Database.CreateExecutionStrategy();
                IActionResult? ketQua = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        var yeuCau = await _context.YeuCauThue
                            .Include(y => y.NguoiDung)
                            .Include(y => y.Phong).ThenInclude(p => p.NhaTro)
                            .FirstOrDefaultAsync(y => y.MaYeuCau == id);

                        if (yeuCau == null)
                        {
                            ketQua = NotFound(ApiResponse<object>.Loi("KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u thuÃª"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                        {
                            ketQua = Forbid();
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (yeuCau.TrangThai != ChoDuyet && yeuCau.TrangThai != DaChapNhan)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("YÃªu cáº§u nÃ y Ä‘Ã£ Ä‘Æ°á»£c xá»­ lÃ½"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var soThangThue = dto.SoThangThue ?? yeuCau.SoThangMuonThue;
                        if (soThangThue < 1 || soThangThue > 60)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Sá»‘ thÃ¡ng thuÃª pháº£i tá»« 1 Ä‘áº¿n 60"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var ngayKetThucHopDong = dto.NgayKetThuc ?? TinhNgayKetThucTheoSoThang(dto.NgayBatDau, soThangThue);

                        if (ngayKetThucHopDong <= dto.NgayBatDau)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("NgÃ y káº¿t thÃºc pháº£i lá»›n hÆ¡n ngÃ y báº¯t Ä‘áº§u"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (dto.TienCoc < 0)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Tiá»n cá»c pháº£i lá»›n hÆ¡n hoáº·c báº±ng 0"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (yeuCau.Phong.GiaPhong <= 0)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("GiÃ¡ thuÃª pháº£i lá»›n hÆ¡n 0"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var phongDangCoHopDongHieuLuc = await _context.HopDong.AnyAsync(h =>
                            h.MaPhong == yeuCau.MaPhong &&
                            (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

                        if (phongDangCoHopDongHieuLuc)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("PhÃ²ng nÃ y Ä‘Ã£ cÃ³ há»£p Ä‘á»“ng cÃ²n hiá»‡u lá»±c"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var nguoiThue = await _context.NguoiThue
                            .FirstOrDefaultAsync(nt => nt.MaNguoiDung == yeuCau.MaNguoiDung && nt.MaPhong == yeuCau.MaPhong);

                        if (nguoiThue == null)
                        {
                            nguoiThue = new NguoiThue
                            {
                                HoTen = string.IsNullOrWhiteSpace(yeuCau.NguoiDung.HoTen) ? yeuCau.NguoiDung.TenDangNhap : yeuCau.NguoiDung.HoTen,
                                Email = yeuCau.NguoiDung.Email,
                                SDT = yeuCau.NguoiDung.SoDienThoai,
                                MaPhong = yeuCau.MaPhong,
                                MaNguoiDung = yeuCau.MaNguoiDung,
                                QuocTich = "Viá»‡t Nam"
                            };

                            _context.NguoiThue.Add(nguoiThue);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            nguoiThue.MaPhong = yeuCau.MaPhong;
                            nguoiThue.MaNguoiDung = yeuCau.MaNguoiDung;
                            await _context.SaveChangesAsync();
                        }

                        var hopDong = new HopDong
                        {
                            MaNguoiThue = nguoiThue.MaNguoiThue,
                            MaPhong = yeuCau.MaPhong,
                            NgayBatDau = dto.NgayBatDau,
                            NgayKetThuc = ngayKetThucHopDong,
                            TienCoc = dto.TienCoc,
                            NoiDung = dto.NoiDung
                        };

                        _context.HopDong.Add(hopDong);
                        await _context.SaveChangesAsync();

                        yeuCau.MaNguoiThue = nguoiThue.MaNguoiThue;
                        yeuCau.MaHopDong = hopDong.MaHopDong;
                        yeuCau.TrangThai = DaLapHopDong;
                        yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
                        yeuCau.NgayXuLy = DateTime.Now;

                        // Chuyển phòng sang trạng thái "Đã thuê" khi có hợp đồng hiệu lực.
                        var trangThaiDaThue = await _context.TrangThai
                            .FirstOrDefaultAsync(t => t.TenTrangThai.Contains("thuê") || t.TenTrangThai.Contains("thue"));
                        if (trangThaiDaThue != null)
                            yeuCau.Phong.MaTrangThai = trangThaiDaThue.MaTrangThai;

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        await _notificationEmailService.GuiEmailYeuCauThueAsync(id, true);

                        ketQua = Ok(ApiResponse<object>.Ok(new
                        {
                            yeuCau.MaYeuCau,
                            nguoiThue.MaNguoiThue,
                            hopDong.MaHopDong,
                            yeuCau.MaPhong,
                            SoThangThue = soThangThue,
                            NgayKetThuc = ngayKetThucHopDong
                        }, "ÄÃ£ cháº¥p nháº­n yÃªu cáº§u vÃ  láº­p há»£p Ä‘á»“ng thÃ nh cÃ´ng"));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return ketQua ?? StatusCode(500, ApiResponse<object>.Loi("KhÃ´ng thá»ƒ xá»­ lÃ½ yÃªu cáº§u thuÃª"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/YeuCauThue/5/tu-choi
        [HttpPost("{id}/tu-choi")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> TuChoiYeuCauThue(int id, [FromBody] TuChoiYeuCauThueDto dto)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var yeuCau = await _context.YeuCauThue
                    .Include(y => y.Phong).ThenInclude(p => p.NhaTro)
                    .FirstOrDefaultAsync(y => y.MaYeuCau == id);

                if (yeuCau == null)
                    return NotFound(ApiResponse<object>.Loi("KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u thuÃª"));

                if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                if (yeuCau.TrangThai != ChoDuyet)
                    return BadRequest(ApiResponse<object>.Loi("Chá»‰ cÃ³ thá»ƒ tá»« chá»‘i yÃªu cáº§u Ä‘ang chá» duyá»‡t"));

                yeuCau.TrangThai = TuChoi;
                yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
                yeuCau.NgayXuLy = DateTime.Now;

                await _context.SaveChangesAsync();

                await _notificationEmailService.GuiEmailYeuCauThueAsync(id, false);
                return Ok(ApiResponse<object>.Ok(null!, "ÄÃ£ tá»« chá»‘i yÃªu cáº§u thuÃª"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/YeuCauThue/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYeuCauThue(int id)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                var yeuCau = await _context.YeuCauThue
                    .Include(y => y.Phong).ThenInclude(p => p.NhaTro)
                    .FirstOrDefaultAsync(y => y.MaYeuCau == id);

                if (yeuCau == null)
                    return NotFound(ApiResponse<object>.Loi("KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u thuÃª"));

                if (role == VaiTroConst.NguoiDung && yeuCau.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                // ÄÃ£ láº­p há»£p Ä‘á»“ng â†’ giá»¯ lá»‹ch sá»­, khÃ´ng thao tÃ¡c
                if (yeuCau.TrangThai == DaLapHopDong)
                    return BadRequest(ApiResponse<object>.Loi(
                        "YÃªu cáº§u Ä‘Ã£ Ä‘Æ°á»£c láº­p há»£p Ä‘á»“ng. KhÃ´ng thá»ƒ xÃ³a Ä‘á»ƒ giá»¯ lá»‹ch sá»­ dá»¯ liá»‡u."));

                // Äang chá» duyá»‡t â†’ XÃ³a cá»©ng (há»§y bá» yÃªu cáº§u chÆ°a xá»­ lÃ½)
                if (yeuCau.TrangThai == ChoDuyet)
                {
                    _context.YeuCauThue.Remove(yeuCau);
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(null!, "ÄÃ£ há»§y yÃªu cáº§u thuÃª thÃ nh cÃ´ng"));
                }

                // ÄÃ£ tá»« chá»‘i / Ä‘Ã£ cháº¥p nháº­n nhÆ°ng chÆ°a láº­p há»£p Ä‘á»“ng â†’ giá»¯ lá»‹ch sá»­
                return Ok(ApiResponse<object>.Ok(null!,
                    $"YÃªu cáº§u thuÃª cÃ³ tráº¡ng thÃ¡i \"{yeuCau.TrangThai}\" Ä‘Ã£ Ä‘Æ°á»£c giá»¯ láº¡i Ä‘á»ƒ lÆ°u lá»‹ch sá»­. " +
                    "Chá»‰ cÃ³ thá»ƒ há»§y yÃªu cáº§u Ä‘ang chá» duyá»‡t."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}

