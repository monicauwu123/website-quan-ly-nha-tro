using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using DoAnSE104.Services;
using DoAnSE104.Services.Interfaces;

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
        private const string ChoNguoiThueXacNhan = "ChoNguoiThueXacNhan";
        private const string NguoiThueTuChoi = "NguoiThueTuChoi";
        private const string DaLapHopDong = "DaLapHopDong";
        private const string HopDongChoXacNhan = "ChoXacNhan";
        private const string HopDongDangHieuLuc = "DangHieuLuc";
        private const string HopDongHuy = "Huy";

        private readonly ApplicationDbContext _context;
        private readonly INotificationEmailService _notificationEmailService;
        private readonly IDeleteValidationService _deleteValidationService;

        public YeuCauThueController(ApplicationDbContext context, INotificationEmailService notificationEmailService, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _notificationEmailService = notificationEmailService;
            _deleteValidationService = deleteValidationService;
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
                    ChoDuyet => "Chờ duyệt",
                    DaChapNhan => "Đã chấp nhận",
                    ChoNguoiThueXacNhan => "Chờ người thuê xác nhận",
                    DaLapHopDong => "Đã lập hợp đồng",
                    NguoiThueTuChoi => "Người thuê từ chối hợp đồng",
                    TuChoi => "Từ chối",
                    _ => y.TrangThai
                },
                y.GhiChuNguoiDung,
                y.GhiChuChuTro,
                y.SoThangMuonThue,
                y.NgayBatDauMongMuon,
                HopDong = y.HopDong == null ? null : new
                {
                    y.HopDong.MaHopDong,
                    y.HopDong.NgayBatDau,
                    y.HopDong.NgayKetThuc,
                    y.HopDong.TienCoc,
                    y.HopDong.NoiDung,
                    y.HopDong.TrangThai
                },
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
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));

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
                    return BadRequest(ApiResponse<object>.Loi("Số tháng muốn thuê phải từ 1 đến 60"));

                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .FirstOrDefaultAsync(p => p.MaPhong == dto.MaPhong);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Phòng không tồn tại"));

                var daCoHopDongHieuLuc = await _context.HopDong
                    .AnyAsync(h => h.Phong.MaPhong == dto.MaPhong &&
                        h.TrangThai != HopDongHuy &&
                        (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));
                if (daCoHopDongHieuLuc)
                    return BadRequest(ApiResponse<object>.Loi("Phòng này đã có hợp đồng hiệu lực hoặc đang chờ người thuê xác nhận"));

                var daCoYeuCauChoDuyet = await _context.YeuCauThue.AnyAsync(y =>
                    y.MaNguoiDung == userId &&
                    y.MaPhong == dto.MaPhong &&
                    (y.TrangThai == ChoDuyet || y.TrangThai == ChoNguoiThueXacNhan));

                if (daCoYeuCauChoDuyet)
                    return BadRequest(ApiResponse<object>.Loi("Bạn đã gửi yêu cầu thuê phòng này và đang chờ chủ trọ duyệt"));

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
                    ApiResponse<YeuCauThue>.Ok(yeuCau, "Gửi yêu cầu thuê thành công"));
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
                            ketQua = NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));
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
                            ketQua = BadRequest(ApiResponse<object>.Loi("Yêu cầu này đã được xử lý"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var soThangThue = dto.SoThangThue ?? yeuCau.SoThangMuonThue;
                        if (soThangThue < 1 || soThangThue > 60)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Số tháng thuê phải từ 1 đến 60"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var ngayKetThucHopDong = dto.NgayKetThuc ?? TinhNgayKetThucTheoSoThang(dto.NgayBatDau, soThangThue);

                        if (ngayKetThucHopDong <= dto.NgayBatDau)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Ngày kết thúc phải lớn hơn ngày bắt đầu"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (dto.TienCoc < 0)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Tiền cọc phải lớn hơn hoặc bằng 0"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        if (yeuCau.Phong.GiaPhong <= 0)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Giá thuê phải lớn hơn 0"));
                            await transaction.RollbackAsync();
                            return;
                        }

                        var phongDangCoHopDongHieuLuc = await _context.HopDong.AnyAsync(h =>
                            h.MaPhong == yeuCau.MaPhong &&
                            h.TrangThai != HopDongHuy &&
                            (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now));

                        if (phongDangCoHopDongHieuLuc)
                        {
                            ketQua = BadRequest(ApiResponse<object>.Loi("Phòng này đã có hợp đồng còn hiệu lực"));
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
                                QuocTich = "Việt Nam"
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
                            NoiDung = dto.NoiDung,
                            TrangThai = HopDongChoXacNhan
                        };

                        _context.HopDong.Add(hopDong);
                        await _context.SaveChangesAsync();

                        yeuCau.MaNguoiThue = nguoiThue.MaNguoiThue;
                        yeuCau.MaHopDong = hopDong.MaHopDong;
                        yeuCau.TrangThai = ChoNguoiThueXacNhan;
                        yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
                        yeuCau.NgayXuLy = DateTime.Now;

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
                            NgayKetThuc = ngayKetThucHopDong,
                            TrangThaiHopDong = hopDong.TrangThai
                        }, "Đã lập hợp đồng, đang chờ người thuê xác nhận điều khoản"));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return ketQua ?? StatusCode(500, ApiResponse<object>.Loi("Không thể xử lý yêu cầu thuê"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/YeuCauThue/5/xac-nhan-hop-dong
        [HttpPost("{id}/xac-nhan-hop-dong")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> XacNhanHopDong(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var yeuCau = await _context.YeuCauThue
                    .Include(y => y.HopDong)
                    .Include(y => y.Phong)
                    .FirstOrDefaultAsync(y => y.MaYeuCau == id);

                if (yeuCau == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));

                if (yeuCau.MaNguoiDung != userId)
                    return Forbid();

                if (yeuCau.TrangThai != ChoNguoiThueXacNhan || yeuCau.HopDong == null)
                    return BadRequest(ApiResponse<object>.Loi("Yêu cầu này không có hợp đồng đang chờ xác nhận"));

                if (yeuCau.HopDong.TrangThai != HopDongChoXacNhan)
                    return BadRequest(ApiResponse<object>.Loi("Hợp đồng này không còn ở trạng thái chờ xác nhận"));

                yeuCau.HopDong.TrangThai = HopDongDangHieuLuc;
                yeuCau.TrangThai = DaLapHopDong;
                yeuCau.NgayXuLy = DateTime.Now;

                var trangThaiDaThue = await _context.TrangThai
                    .FirstOrDefaultAsync(t => t.TenTrangThai.Contains("thuê") || t.TenTrangThai.Contains("thue"));
                if (trangThaiDaThue != null)
                    yeuCau.Phong.MaTrangThai = trangThaiDaThue.MaTrangThai;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    yeuCau.MaYeuCau,
                    yeuCau.MaHopDong,
                    TrangThaiHopDong = yeuCau.HopDong.TrangThai
                }, "Đã xác nhận hợp đồng. Hợp đồng bắt đầu có hiệu lực."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/YeuCauThue/5/tu-choi-hop-dong
        [HttpPost("{id}/tu-choi-hop-dong")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> TuChoiHopDong(int id, [FromBody] TuChoiHopDongYeuCauThueDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var yeuCau = await _context.YeuCauThue
                    .Include(y => y.HopDong)
                    .FirstOrDefaultAsync(y => y.MaYeuCau == id);

                if (yeuCau == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));

                if (yeuCau.MaNguoiDung != userId)
                    return Forbid();

                if (yeuCau.TrangThai != ChoNguoiThueXacNhan || yeuCau.HopDong == null)
                    return BadRequest(ApiResponse<object>.Loi("Yêu cầu này không có hợp đồng đang chờ xác nhận"));

                yeuCau.HopDong.TrangThai = HopDongHuy;
                yeuCau.TrangThai = NguoiThueTuChoi;
                yeuCau.GhiChuNguoiDung = string.IsNullOrWhiteSpace(dto.GhiChuNguoiDung)
                    ? yeuCau.GhiChuNguoiDung
                    : dto.GhiChuNguoiDung;
                yeuCau.NgayXuLy = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null!, "Đã từ chối hợp đồng. Chủ trọ có thể điều chỉnh và duyệt lại yêu cầu mới."));
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
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));

                if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                if (yeuCau.TrangThai != ChoDuyet)
                    return BadRequest(ApiResponse<object>.Loi("Chỉ có thể từ chối yêu cầu đang chờ duyệt"));

                yeuCau.TrangThai = TuChoi;
                yeuCau.GhiChuChuTro = dto.GhiChuChuTro;
                yeuCau.NgayXuLy = DateTime.Now;

                await _context.SaveChangesAsync();

                await _notificationEmailService.GuiEmailYeuCauThueAsync(id, false);
                return Ok(ApiResponse<object>.Ok(null!, "Đã từ chối yêu cầu thuê"));
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
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy yêu cầu thuê"));

                if (role == VaiTroConst.NguoiDung && yeuCau.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro && yeuCau.Phong.NhaTro.MaChuTro != userId)
                    return Forbid();

                var result = await _deleteValidationService.DeleteYeuCauThueAsync(id);
                return this.ToActionResult(result);

                // Đã lập hợp đồng → giữ lịch sử, không thao tác
                if (yeuCau.TrangThai == DaLapHopDong)
                    return BadRequest(ApiResponse<object>.Loi(
                        "Yêu cầu đã được lập hợp đồng. Không thể xóa để giữ lịch sử dữ liệu."));

                // Đang chờ duyệt → Xóa cứng (hủy bỏ yêu cầu chưa xử lý)
                if (yeuCau.TrangThai == ChoDuyet)
                {
                    _context.YeuCauThue.Remove(yeuCau);
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(null!, "Đã hủy yêu cầu thuê thành công"));
                }

                // Đã từ chối / đã chấp nhận nhưng chưa lập hợp đồng → giữ lịch sử
                return Ok(ApiResponse<object>.Ok(null!,
                    $"Yêu cầu thuê có trạng thái \"{yeuCau.TrangThai}\" đã được giữ lại để lưu lịch sử. " +
                    "Chỉ có thể hủy yêu cầu đang chờ duyệt."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
