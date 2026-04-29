using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;

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

        private async Task<List<DichVu>> LayDichVuHopLeTheoPhongAsync(int maPhong, List<int>? maDichVuSuDung)
        {
            if (maDichVuSuDung == null || maDichVuSuDung.Count == 0)
                return new List<DichVu>();

            var distinctIds = maDichVuSuDung.Distinct().ToList();

            var maChuTro = await _context.Phong
                .Where(p => p.MaPhong == maPhong)
                .Select(p => p.NhaTro.MaChuTro)
                .FirstOrDefaultAsync();

            return await _context.DichVu
                .Where(dv => distinctIds.Contains(dv.MaDichVu)
                    && (dv.MaChuTro == maChuTro || dv.MaChuTro == null))
                .ToListAsync();
        }

        private static string TaoLoaiKhoanDichVu(DichVu dichVu)
        {
            var ten = string.IsNullOrWhiteSpace(dichVu.TenDichVu)
                ? "Dịch vụ"
                : dichVu.TenDichVu.Trim();

            var loaiKhoan = $"DichVu:{dichVu.MaDichVu}:{ten}";
            return loaiKhoan.Length <= 50 ? loaiKhoan : loaiKhoan.Substring(0, 50);
        }

        private static string LayTenDichVuTuLoaiKhoan(string loaiKhoan)
        {
            if (string.IsNullOrWhiteSpace(loaiKhoan))
                return "Dịch vụ";

            var parts = loaiKhoan.Split(':', 3);
            return parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2])
                ? parts[2]
                : "Dịch vụ";
        }

        private async Task CapNhatChiTietHoaDonDichVuAsync(int maHoaDon, List<DichVu> dichVuSuDung)
        {
            var chiTietDichVuCu = await _context.ChiTietHoaDon
                .Where(ct => ct.MaHoaDon == maHoaDon && ct.LoaiKhoan.StartsWith("DichVu"))
                .ToListAsync();

            if (chiTietDichVuCu.Count > 0)
                _context.ChiTietHoaDon.RemoveRange(chiTietDichVuCu);

            foreach (var dichVu in dichVuSuDung)
            {
                _context.ChiTietHoaDon.Add(new ChiTietHoaDon
                {
                    MaHoaDon = maHoaDon,
                    LoaiKhoan = TaoLoaiKhoanDichVu(dichVu),
                    SoTien = (decimal)dichVu.Tiendichvu
                });
            }
        }

        private static string LayTrangThaiThanhToan(decimal tongTien, decimal daThanhToan)
        {
            if (daThanhToan <= 0) return "Chưa thanh toán";
            if (daThanhToan < tongTien) return "Thanh toán một phần";
            return "Đã thanh toán";
        }

        private static string TaoNoiDungChuyenKhoan(HoaDon hoaDon, User? chuTro)
        {
            var macDinh = chuTro?.NoiDungChuyenKhoanMacDinh;
            if (!string.IsNullOrWhiteSpace(macDinh))
            {
                return macDinh
                    .Replace("{MaHoaDon}", hoaDon.MaHoaDon.ToString())
                    .Replace("{KyHoaDon}", hoaDon.KyHoaDon ?? "")
                    .Replace("{TenPhong}", hoaDon.Phong?.TenPhong ?? "")
                    .Trim();
            }

            return $"Thanh toan hoa don {hoaDon.MaHoaDon} phong {hoaDon.Phong?.TenPhong ?? hoaDon.MaPhong.ToString()} ky {hoaDon.KyHoaDon}";
        }

        private static string? TaoVietQrUrl(User? chuTro, decimal soTien, string noiDung)
        {
            if (chuTro == null || string.IsNullOrWhiteSpace(chuTro.MaNganHang) || string.IsNullOrWhiteSpace(chuTro.SoTaiKhoan))
                return null;

            var bank = Uri.EscapeDataString(chuTro.MaNganHang.Trim());
            var account = Uri.EscapeDataString(chuTro.SoTaiKhoan.Trim());
            var amount = Math.Max(0, decimal.ToInt64(decimal.Round(soTien, 0, MidpointRounding.AwayFromZero)));
            var addInfo = Uri.EscapeDataString(noiDung ?? string.Empty);
            var accountName = Uri.EscapeDataString(chuTro.TenChuTaiKhoan ?? chuTro.HoTen ?? string.Empty);

            return $"https://img.vietqr.io/image/{bank}-{account}-compact2.png?amount={amount}&addInfo={addInfo}&accountName={accountName}";
        }

        // GET: api/HoaDon
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetAllHoaDon()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<HoaDon> query = _context.HoaDon
                .Include(h => h.Phong)
                    .ThenInclude(p => p.NhaTro)
                        .ThenInclude(n => n.ChuTro)
                .Include(h => h.NguoiThue)
                .Include(h => h.ChiSoDien)
                .Include(h => h.ChiSoNuoc)
                .Include(h => h.ChiTietHoaDon);

            if (role == VaiTroConst.ChuTro)
            {
                query = query.Where(h => h.Phong.NhaTro.MaChuTro == userId);
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                query = query.Where(h => h.NguoiThue.MaNguoiDung == userId);
            }

            var danhSachHoaDon = await query
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();

            var maHoaDons = danhSachHoaDon.Select(h => h.MaHoaDon).ToList();
            var thanhToanTheoHoaDon = await _context.ThanhToan
                .Where(t => maHoaDons.Contains(t.MaHoaDon))
                .GroupBy(t => t.MaHoaDon)
                .Select(g => new { MaHoaDon = g.Key, DaThanhToan = g.Sum(x => x.TongTien) })
                .ToDictionaryAsync(x => x.MaHoaDon, x => x.DaThanhToan);

            var hoaDons = danhSachHoaDon.Select(h =>
            {
                var chiTietDichVu = h.ChiTietHoaDon?
                    .Where(ct => ct.LoaiKhoan.StartsWith("DichVu"))
                    .ToList() ?? new List<ChiTietHoaDon>();

                var daThanhToan = thanhToanTheoHoaDon.TryGetValue(h.MaHoaDon, out var paid) ? paid : 0m;
                var conLai = Math.Max(h.TongTien - daThanhToan, 0m);
                var chuTro = h.Phong?.NhaTro?.ChuTro;
                var noiDungChuyenKhoan = TaoNoiDungChuyenKhoan(h, chuTro);

                return new HoaDonDto
                {
                    MaHoaDon = h.MaHoaDon,
                    MaNguoiThue = h.MaNguoiThue,
                    MaPhong = h.MaPhong,
                    TenPhong = h.Phong?.TenPhong ?? "---",
                    TenNguoiThue = h.NguoiThue?.HoTen ?? "---",
                    NgayLap = h.NgayLap,
                    KyHoaDon = h.KyHoaDon,
                    TienPhong = h.Phong?.GiaPhong ?? 0m,
                    TienDien = h.ChiSoDien?.TienDien ?? 0m,
                    TienNuoc = h.ChiSoNuoc?.TienNuoc ?? 0m,
                    TienDichVu = chiTietDichVu.Sum(ct => ct.SoTien),
                    TienPhatSinhKhac = h.TienPhatSinhKhac,
                    TongTien = h.TongTien,
                    DaThanhToan = daThanhToan,
                    ConLai = conLai,
                    TrangThaiThanhToan = LayTrangThaiThanhToan(h.TongTien, daThanhToan),
                    MaChuTro = h.Phong?.NhaTro?.MaChuTro,
                    TenChuTro = chuTro?.HoTen,
                    TenNganHang = chuTro?.TenNganHang,
                    MaNganHang = chuTro?.MaNganHang,
                    SoTaiKhoan = chuTro?.SoTaiKhoan,
                    TenChuTaiKhoan = chuTro?.TenChuTaiKhoan,
                    NoiDungChuyenKhoan = noiDungChuyenKhoan,
                    QrThanhToanUrl = TaoVietQrUrl(chuTro, conLai, noiDungChuyenKhoan),
                    DichVuSuDung = chiTietDichVu.Select(ct => LayTenDichVuTuLoaiKhoan(ct.LoaiKhoan)).ToList()
                };
            }).ToList();

            return Ok(hoaDons);
        }

        // GET: api/HoaDon/GetThongTinPhong/5
        [HttpGet("GetThongTinPhong/{phongId}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> GetThongTinPhong(int phongId)
        {
            var phong = await _context.Phong
                .Include(p => p.NhaTro)
                .FirstOrDefaultAsync(p => p.MaPhong == phongId);

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
                return BadRequest(ApiResponse<object>.Loi("Phòng chưa có hợp đồng thuê hợp lệ"));

            var nguoiThue = await _context.NguoiThue.FindAsync(hopDong.MaNguoiThue);
            if (nguoiThue == null)
                return BadRequest(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

            var chiSoDien = await _context.ChiSoDien
                .Where(cd => cd.MaPhong == phongId)
                .OrderByDescending(cd => cd.NgayThangDien)
                .FirstOrDefaultAsync();

            if (chiSoDien == null)
                return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số điện cho phòng này"));

            var chiSoNuoc = await _context.ChiSoNuoc
                .Where(cn => cn.MaPhong == phongId)
                .OrderByDescending(cn => cn.NgayThangNuoc)
                .FirstOrDefaultAsync();

            if (chiSoNuoc == null)
                return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số nước cho phòng này"));

            var danhSachDichVu = await _context.DichVu
                .Where(dv => dv.MaChuTro == phong.NhaTro.MaChuTro || dv.MaChuTro == null)
                .OrderBy(dv => dv.TenDichVu)
                .Select(dv => new
                {
                    dv.MaDichVu,
                    dv.TenDichVu,
                    TienDichVu = (decimal)dv.Tiendichvu
                })
                .ToListAsync();

            decimal tongTienHoaDon = phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc;

            return Ok(new
            {
                Phong = new { phong.MaPhong, phong.TenPhong, GiaPhong = phong.GiaPhong },
                NguoiThue = new { nguoiThue.MaNguoiThue, nguoiThue.HoTen },
                TienDien = chiSoDien.TienDien,
                TienNuoc = chiSoNuoc.TienNuoc,
                DanhSachDichVu = danhSachDichVu,
                TongTienDichVu = 0m,
                TongTienHoaDon = tongTienHoaDon,
                MaDien = chiSoDien.MaDien,
                MaNuoc = chiSoNuoc.MaNuoc
            });
        }

        // GET: api/HoaDon/GetPhongChuaCoHoaDonTrongThang
        [HttpGet("GetPhongChuaCoHoaDonTrongThang")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> GetPhongChuaCoHoaDonTrongThang([FromQuery] int thang, [FromQuery] int nam)
        {
            if (thang < 1 || thang > 12 || nam < 1900 || nam > 9999)
                return BadRequest(ApiResponse<object>.Loi("Tháng năm không hợp lệ"));

            var kyHoaDon = $"{nam:D4}-{thang:D2}";

            var phongDaCoHoaDon = await _context.HoaDon
                .Where(hd => hd.KyHoaDon == kyHoaDon)
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

        // POST: api/HoaDon
        [HttpPost]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> CreateHoaDon([FromBody] HoaDonCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .FirstOrDefaultAsync(p => p.MaPhong == request.MaPhong);

                if (phong == null)
                    return BadRequest(ApiResponse<object>.Loi("Phòng không tồn tại"));

                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(request.MaPhong))
                    return Forbid();

                var nguoiThue = await _context.NguoiThue.FindAsync(request.MaNguoiThue);
                if (nguoiThue == null)
                    return BadRequest(ApiResponse<object>.Loi("Người thuê không tồn tại"));

                var hopDong = await _context.HopDong
                    .Where(hd => hd.MaPhong == request.MaPhong
                        && hd.MaNguoiThue == request.MaNguoiThue
                        && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now))
                    .FirstOrDefaultAsync();

                if (hopDong == null)
                    return BadRequest(ApiResponse<object>.Loi("Không tìm thấy hợp đồng thuê hợp lệ cho phòng này"));

                var chiSoDien = await _context.ChiSoDien
                    .Where(cd => cd.MaPhong == request.MaPhong)
                    .OrderByDescending(cd => cd.NgayThangDien)
                    .FirstOrDefaultAsync();

                if (chiSoDien == null)
                    return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số điện cho phòng này"));

                var chiSoNuoc = await _context.ChiSoNuoc
                    .Where(cn => cn.MaPhong == request.MaPhong)
                    .OrderByDescending(cn => cn.NgayThangNuoc)
                    .FirstOrDefaultAsync();

                if (chiSoNuoc == null)
                    return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số nước cho phòng này"));

                var dichVuSuDung = await LayDichVuHopLeTheoPhongAsync(request.MaPhong, request.MaDichVuSuDung);
                var soLuongDichVuGuiLen = request.MaDichVuSuDung?.Distinct().Count() ?? 0;
                if (dichVuSuDung.Count != soLuongDichVuGuiLen)
                    return BadRequest(ApiResponse<object>.Loi("Có dịch vụ không tồn tại hoặc không thuộc chủ trọ của phòng này"));

                decimal tongTienDichVu = dichVuSuDung.Sum(dv => (decimal)dv.Tiendichvu);
                decimal tongTien = phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc + tongTienDichVu + request.TienPhatSinhKhac;

                var loiValidation = await ValidateHoaDon(0, request.MaPhong, request.KyHoaDon, tongTien);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var hoaDon = new HoaDon
                {
                    MaPhong = request.MaPhong,
                    MaNguoiThue = request.MaNguoiThue,
                    MaDien = chiSoDien.MaDien,
                    MaNuoc = chiSoNuoc.MaNuoc,
                    NgayLap = request.NgayLap,
                    KyHoaDon = request.KyHoaDon,
                    TongTien = tongTien,
                    TienPhatSinhKhac = request.TienPhatSinhKhac
                };

                _context.HoaDon.Add(hoaDon);
                await _context.SaveChangesAsync();

                await CapNhatChiTietHoaDonDichVuAsync(hoaDon.MaHoaDon, dichVuSuDung);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    hoaDon.MaHoaDon,
                    hoaDon.TongTien,
                    hoaDon.KyHoaDon,
                    TienDichVu = tongTienDichVu
                }, "Tạo hóa đơn thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi($"Lỗi khi tạo hóa đơn: {ex.Message}"));
            }
        }

        // PUT: api/HoaDon/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> UpdateHoaDon(int id, [FromBody] HoaDonUpdateDto dto)
        {
            try
            {
                if (id != dto.MaHoaDon)
                    return BadRequest(ApiResponse<object>.Loi("Mã hóa đơn không khớp"));

                var hoaDon = await _context.HoaDon.FindAsync(id);
                if (hoaDon == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy hóa đơn"));

                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenHoaDon(id))
                    return Forbid();

                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .FirstOrDefaultAsync(p => p.MaPhong == dto.MaPhong);

                if (phong == null)
                    return BadRequest(ApiResponse<object>.Loi("Phòng không tồn tại"));

                if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenPhong(dto.MaPhong))
                    return Forbid();

                var nguoiThue = await _context.NguoiThue.FindAsync(dto.MaNguoiThue);
                if (nguoiThue == null)
                    return BadRequest(ApiResponse<object>.Loi("Người thuê không tồn tại"));

                var hopDong = await _context.HopDong
                    .Where(hd => hd.MaPhong == dto.MaPhong
                        && hd.MaNguoiThue == dto.MaNguoiThue
                        && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now))
                    .FirstOrDefaultAsync();

                if (hopDong == null)
                    return BadRequest(ApiResponse<object>.Loi("Không tìm thấy hợp đồng thuê hợp lệ cho phòng này"));

                var chiSoDien = await _context.ChiSoDien
                    .Where(cd => cd.MaPhong == dto.MaPhong)
                    .OrderByDescending(cd => cd.NgayThangDien)
                    .FirstOrDefaultAsync();

                if (chiSoDien == null)
                    return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số điện cho phòng này"));

                var chiSoNuoc = await _context.ChiSoNuoc
                    .Where(cn => cn.MaPhong == dto.MaPhong)
                    .OrderByDescending(cn => cn.NgayThangNuoc)
                    .FirstOrDefaultAsync();

                if (chiSoNuoc == null)
                    return BadRequest(ApiResponse<object>.Loi("Chưa có chỉ số nước cho phòng này"));

                var dichVuSuDung = await LayDichVuHopLeTheoPhongAsync(dto.MaPhong, dto.MaDichVuSuDung);
                var soLuongDichVuGuiLen = dto.MaDichVuSuDung?.Distinct().Count() ?? 0;
                if (dichVuSuDung.Count != soLuongDichVuGuiLen)
                    return BadRequest(ApiResponse<object>.Loi("Có dịch vụ không tồn tại hoặc không thuộc chủ trọ của phòng này"));

                decimal tongTienDichVu = dichVuSuDung.Sum(dv => (decimal)dv.Tiendichvu);
                decimal tongTien = phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc + tongTienDichVu + dto.TienPhatSinhKhac;

                var loiValidation = await ValidateHoaDon(id, dto.MaPhong, dto.KyHoaDon, tongTien);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                hoaDon.MaNguoiThue = dto.MaNguoiThue;
                hoaDon.MaPhong = dto.MaPhong;
                hoaDon.MaDien = chiSoDien.MaDien;
                hoaDon.MaNuoc = chiSoNuoc.MaNuoc;
                hoaDon.NgayLap = dto.NgayLap;
                hoaDon.KyHoaDon = dto.KyHoaDon;
                hoaDon.TienPhatSinhKhac = dto.TienPhatSinhKhac;
                hoaDon.TongTien = tongTien;

                await CapNhatChiTietHoaDonDichVuAsync(hoaDon.MaHoaDon, dichVuSuDung);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    hoaDon.MaHoaDon,
                    hoaDon.TongTien,
                    hoaDon.KyHoaDon,
                    TienDichVu = tongTienDichVu
                }, "Cập nhật hóa đơn thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi($"Lỗi khi cập nhật hóa đơn: {ex.Message}"));
            }
        }

        // DELETE: api/HoaDon/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ChuTro")]
        public async Task<IActionResult> DeleteHoaDon(int id)
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy dữ liệu"));

            if (GetCurrentRole() == VaiTroConst.ChuTro && !await ChuTroCoQuyenHoaDon(id))
                return Forbid();

            var chiTiet = await _context.ChiTietHoaDon
                .Where(ct => ct.MaHoaDon == id)
                .ToListAsync();

            if (chiTiet.Count > 0)
                _context.ChiTietHoaDon.RemoveRange(chiTiet);

            _context.HoaDon.Remove(hoaDon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
