using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
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
    public class ThanhToanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IDeleteValidationService _deleteValidationService;

        public ThanhToanController(ApplicationDbContext context, Cloudinary cloudinary, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _cloudinary = cloudinary;
            _deleteValidationService = deleteValidationService;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<List<int>> GetMaPhongCuaChuTro(int userId)
        {
            var maNhaTroList = await _context.NhaTro
                .Where(n => n.MaChuTro == userId)
                .Select(n => n.MaNhaTro)
                .ToListAsync();

            return await _context.Phong
                .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                .Select(p => p.MaPhong)
                .ToListAsync();
        }

        private async Task<bool> CoQuyenThanhToan(ThanhToan thanhToan)
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.Admin) return true;

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                var maPhong = await _context.HoaDon
                    .Where(h => h.MaHoaDon == thanhToan.MaHoaDon)
                    .Select(h => h.MaPhong)
                    .FirstOrDefaultAsync();
                return maPhongList.Contains(maPhong);
            }

            return await _context.NguoiThue
                .AnyAsync(nt => nt.MaNguoiThue == thanhToan.MaNguoiThue && nt.MaNguoiDung == userId);
        }

        private async Task<string?> ValidateThanhToan(ThanhToan thanhToan)
        {
            if (thanhToan.TongTien < 0)
                return "Số tiền thanh toán phải lớn hơn hoặc bằng 0";

            var hoaDon = await _context.HoaDon.FindAsync(thanhToan.MaHoaDon);
            if (hoaDon == null) return "Không tìm thấy hóa đơn";

            var nguoiThue = await _context.NguoiThue.FindAsync(thanhToan.MaNguoiThue);
            if (nguoiThue == null) return "Không tìm thấy người thuê";

            if (hoaDon.MaNguoiThue != thanhToan.MaNguoiThue)
                return "Người thuê không khớp với hóa đơn";

            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(GetCurrentUserId());
                if (!maPhongList.Contains(hoaDon.MaPhong))
                    return "Bạn chỉ được thao tác thanh toán thuộc nhà trọ của mình";
            }

            var tongDaThanhToan = await _context.ThanhToan
                .Where(t => t.MaHoaDon == thanhToan.MaHoaDon && t.MaThanhToan != thanhToan.MaThanhToan)
                .SumAsync(t => t.TongTien);

            if (tongDaThanhToan + thanhToan.TongTien > hoaDon.TongTien)
                return "Số tiền thanh toán vượt quá tổng tiền hóa đơn";

            return null;
        }

        private static string MapTenTrangThaiXacNhan(string? trangThai) => trangThai switch
        {
            "ChoXacNhan" => "Chờ xác nhận",
            "DaXacNhan"  => "Đã xác nhận",
            "TuChoi"     => "Bị từ chối",
            _            => "Chưa gửi biên lai"
        };

        private ThanhToanDto ToDto(ThanhToan t) => new ThanhToanDto
        {
            MaThanhToan        = t.MaThanhToan,
            MaHoaDon           = t.MaHoaDon,
            MaNguoiThue        = t.MaNguoiThue,
            TenNguoiThue       = t.NguoiThue?.HoTen ?? "",
            NgayThanhToan      = t.NgayThanhToan,
            TongTien           = t.TongTien,
            HinhThucThanhToan  = t.HinhThucThanhToan,
            GhiChu             = t.GhiChu,
            HinhAnhBienLai     = t.HinhAnhBienLai,
            MaGiaoDich         = t.MaGiaoDich,
            TrangThaiXacNhan   = t.TrangThaiXacNhan,
            TenTrangThaiXacNhan = MapTenTrangThaiXacNhan(t.TrangThaiXacNhan),
            LyDoTuChoi         = t.LyDoTuChoi,
            NguoiXacNhanId     = t.NguoiXacNhanId,
            TenNguoiXacNhan    = t.NguoiXacNhan?.HoTen,
            NgayXacNhan        = t.NgayXacNhan
        };

        // ── Helper cập nhật TrangThai HoaDon sau xác nhận ────────────────────

        private async Task CapNhatTrangThaiHoaDon(int maHoaDon)
        {
            var hoaDon = await _context.HoaDon.FindAsync(maHoaDon);
            if (hoaDon == null) return;

            // Chỉ tính các thanh toán đã được xác nhận
            var tongDaXacNhan = await _context.ThanhToan
                .Where(t => t.MaHoaDon == maHoaDon && t.TrangThaiXacNhan == "DaXacNhan")
                .SumAsync(t => t.TongTien);

            if (tongDaXacNhan >= hoaDon.TongTien)
                hoaDon.TrangThai = "DaThanhToan";
            else if (tongDaXacNhan > 0)
                hoaDon.TrangThai = "ThanhToanMotPhan"; // Ghi chú: thêm trạng thái này nếu cần
            else
                hoaDon.TrangThai = "ChuaThanhToan";

            await _context.SaveChangesAsync();
        }

        // ── Upload ảnh biên lai lên Cloudinary ──────────────────────────────

        private async Task<string?> UploadBienLaiAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) return null;
            if (file.Length > 10 * 1024 * 1024) return null;

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "bien_lai",
                Transformation = new Transformation().Width(1200).Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.StatusCode == System.Net.HttpStatusCode.OK && result.SecureUrl != null)
                return result.SecureUrl.AbsoluteUri;

            return null;
        }

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINTS CŨ (GIỮ NGUYÊN, không phá chức năng cũ)
        // ════════════════════════════════════════════════════════════════════

        // GET: api/ThanhToan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ThanhToanDto>>> GetThanhToan()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<ThanhToan> query = _context.ThanhToan
                .Include(t => t.HoaDon)
                .Include(t => t.NguoiThue)
                .Include(t => t.NguoiXacNhan);

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                query = query.Where(t => maPhongList.Contains(t.HoaDon.MaPhong));
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                query = query.Where(t => t.NguoiThue.MaNguoiDung == userId);
            }

            var list = await query.ToListAsync();
            return Ok(list.Select(ToDto));
        }

        // GET: api/ThanhToan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThanhToanDto>> GetThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan
                .Include(t => t.HoaDon)
                .Include(t => t.NguoiThue)
                .Include(t => t.NguoiXacNhan)
                .FirstOrDefaultAsync(t => t.MaThanhToan == id);

            if (thanhToan == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(thanhToan))
                return Forbid();

            return Ok(ToDto(thanhToan));
        }

        // GET: api/ThanhToan/HoaDon/5
        [HttpGet("HoaDon/{hoaDonId}")]
        public async Task<ActionResult<IEnumerable<ThanhToanDto>>> GetThanhToanByHoaDon(int hoaDonId)
        {
            var hoaDon = await _context.HoaDon.FindAsync(hoaDonId);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy hóa đơn"));

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                if (!maPhongList.Contains(hoaDon.MaPhong)) return Forbid();
            }
            else if (role == VaiTroConst.NguoiDung)
            {
                var coQuyen = await _context.NguoiThue
                    .AnyAsync(nt => nt.MaNguoiThue == hoaDon.MaNguoiThue && nt.MaNguoiDung == userId);
                if (!coQuyen) return Forbid();
            }

            var list = await _context.ThanhToan
                .Include(t => t.HoaDon)
                .Include(t => t.NguoiThue)
                .Include(t => t.NguoiXacNhan)
                .Where(t => t.MaHoaDon == hoaDonId)
                .ToListAsync();

            return Ok(list.Select(ToDto));
        }

        // POST: api/ThanhToan  (chủ trọ / admin tạo thanh toán trực tiếp - giữ nguyên)
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<ThanhToanDto>> PostThanhToan(ThanhToan thanhToan)
        {
            var loiValidation = await ValidateThanhToan(thanhToan);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            thanhToan.NgayThanhToan = DateTime.Now;
            // Thanh toán do chủ trọ nhập = tự động xác nhận
            thanhToan.TrangThaiXacNhan = "DaXacNhan";
            thanhToan.NguoiXacNhanId = GetCurrentUserId();
            thanhToan.NgayXacNhan = DateTime.Now;

            _context.ThanhToan.Add(thanhToan);
            await _context.SaveChangesAsync();
            await CapNhatTrangThaiHoaDon(thanhToan.MaHoaDon);

            var saved = await _context.ThanhToan
                .Include(t => t.NguoiThue)
                .Include(t => t.NguoiXacNhan)
                .Include(t => t.HoaDon)
                .FirstAsync(t => t.MaThanhToan == thanhToan.MaThanhToan);

            return CreatedAtAction(nameof(GetThanhToan), new { id = saved.MaThanhToan },
                ApiResponse<ThanhToanDto>.Ok(ToDto(saved), "Thêm thanh toán thành công"));
        }

        // PUT: api/ThanhToan/5 (chủ trọ / admin sửa - giữ nguyên)
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutThanhToan(int id, ThanhToan thanhToan)
        {
            if (id != thanhToan.MaThanhToan)
                return BadRequest(ApiResponse<object>.Loi("Mã thanh toán không khớp"));

            var existing = await _context.ThanhToan.AsNoTracking().FirstOrDefaultAsync(t => t.MaThanhToan == id);
            if (existing == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(existing))
                return Forbid();

            var loiValidation = await ValidateThanhToan(thanhToan);
            if (loiValidation != null)
                return BadRequest(ApiResponse<object>.Loi(loiValidation));

            _context.Entry(thanhToan).State = EntityState.Modified;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ThanhToan.Any(e => e.MaThanhToan == id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));
                throw;
            }

            return Ok(ApiResponse<ThanhToan>.Ok(thanhToan, "Cập nhật thanh toán thành công"));
        }

        // DELETE: api/ThanhToan/5 (giữ nguyên)
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan.FindAsync(id);
            if (thanhToan == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (!await CoQuyenThanhToan(thanhToan))
                return Forbid();

            if (GetCurrentRole() == VaiTroConst.ChuTro && thanhToan.TrangThaiXacNhan != "ChoXacNhan")
                return BadRequest(ApiResponse<object>.Loi("Chủ trọ chỉ có thể hủy thanh toán đang chờ xác nhận."));

            var result = await _deleteValidationService.DeleteThanhToanAsync(id);
            return this.ToActionResult(result);

            if (GetCurrentRole() == VaiTroConst.ChuTro)
                return BadRequest(ApiResponse<object>.Loi(
                    "Chủ trọ không thể xóa thanh toán đã ghi nhận. " +
                    "Chỉ Admin mới có quyền xóa thanh toán nhập nhầm. " +
                    "Nếu cần điều chỉnh, vui lòng liên hệ quản trị viên."));

            _context.ThanhToan.Remove(thanhToan);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null!, "Đã xóa thanh toán thành công (chỉ dùng khi nhập nhầm)"));
        }

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINTS MỚI: Gửi biên lai (NguoiDung)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POST api/ThanhToan/gui-bien-lai
        /// Người dùng gửi biên lai để xác nhận thanh toán hóa đơn.
        /// Dùng multipart/form-data: các field text + file "anhBienLai".
        /// </summary>
        [HttpPost("gui-bien-lai")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ThanhToanDto>> GuiBienLai([FromForm] GuiBienLaiDto dto)
        {
            var userId = GetCurrentUserId();
            var anhBienLai = dto.AnhBienLai;

            // 1. Kiểm tra hóa đơn tồn tại
            var hoaDon = await _context.HoaDon.FindAsync(dto.MaHoaDon);
            if (hoaDon == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy hóa đơn"));

            // 2. Kiểm tra người dùng có phải người thuê của hóa đơn đó không
            var nguoiThue = await _context.NguoiThue
                .FirstOrDefaultAsync(nt => nt.MaNguoiThue == hoaDon.MaNguoiThue && nt.MaNguoiDung == userId);
            if (nguoiThue == null)
                return Forbid(); // Không phải hóa đơn của mình

            // 3. Hóa đơn phải đang ở trạng thái chưa thanh toán hoặc thiếu
            if (hoaDon.TrangThai == "DaThanhToan" || hoaDon.TrangThai == "Huy")
                return BadRequest(ApiResponse<object>.Loi(
                    "Hóa đơn này đã thanh toán hoặc đã hủy, không thể gửi biên lai"));

            // 4. Kiểm tra xem đã có biên lai chờ xác nhận chưa
            var daCoBienLai = await _context.ThanhToan
                .AnyAsync(t => t.MaHoaDon == dto.MaHoaDon && t.TrangThaiXacNhan == "ChoXacNhan");
            if (daCoBienLai)
                return BadRequest(ApiResponse<object>.Loi(
                    "Đã có biên lai đang chờ xác nhận cho hóa đơn này. " +
                    "Vui lòng đợi chủ trọ xác nhận trước khi gửi thêm."));

            // 5. Validate số tiền
            if (dto.TongTien <= 0)
                return BadRequest(ApiResponse<object>.Loi("Số tiền phải lớn hơn 0"));

            // 6. Upload ảnh biên lai (nếu có)
            var tongDaXacNhan = await _context.ThanhToan
                .Where(t => t.MaHoaDon == dto.MaHoaDon && t.TrangThaiXacNhan == "DaXacNhan")
                .SumAsync(t => t.TongTien);
            var conLai = Math.Max(hoaDon.TongTien - tongDaXacNhan, 0m);
            if (conLai <= 0)
                return BadRequest(ApiResponse<object>.Loi("Hóa đơn này đã được thanh toán đủ"));

            var kieuThanhToan = string.IsNullOrWhiteSpace(dto.KieuThanhToan)
                ? "ThanhToanHet"
                : dto.KieuThanhToan.Trim();
            if (kieuThanhToan == "ThanhToanHet")
            {
                dto.TongTien = conLai;
            }
            else if (kieuThanhToan == "MotPhan")
            {
                if (dto.TongTien >= conLai)
                    return BadRequest(ApiResponse<object>.Loi("Thanh toán một phần phải nhỏ hơn số tiền còn lại. Nếu trả đủ, vui lòng chọn Thanh toán hết."));
            }
            else
            {
                return BadRequest(ApiResponse<object>.Loi("Kiểu thanh toán không hợp lệ"));
            }

            if (dto.TongTien > conLai)
                return BadRequest(ApiResponse<object>.Loi("Số tiền gửi biên lai vượt quá số tiền còn lại của hóa đơn"));

            string? urlBienLai = null;
            if (anhBienLai != null && anhBienLai.Length > 0)
            {
                urlBienLai = await UploadBienLaiAsync(anhBienLai);
                if (urlBienLai == null)
                    return BadRequest(ApiResponse<object>.Loi(
                        "Upload ảnh biên lai thất bại. Chỉ chấp nhận jpg/jpeg/png/gif/webp, tối đa 10MB"));
            }

            // 7. Tạo bản ghi ThanhToan với trạng thái "ChoXacNhan"
            var thanhToan = new ThanhToan
            {
                MaHoaDon           = dto.MaHoaDon,
                MaNguoiThue        = nguoiThue.MaNguoiThue,
                NgayThanhToan      = DateTime.Now,
                TongTien           = dto.TongTien,
                HinhThucThanhToan  = dto.HinhThucThanhToan ?? "ChuyenKhoan",
                GhiChu             = dto.GhiChu,
                MaGiaoDich         = dto.MaGiaoDich,
                HinhAnhBienLai     = urlBienLai,
                TrangThaiXacNhan   = "ChoXacNhan"
            };

            _context.ThanhToan.Add(thanhToan);
            await _context.SaveChangesAsync();

            var saved = await _context.ThanhToan
                .Include(t => t.NguoiThue)
                .Include(t => t.HoaDon)
                .FirstAsync(t => t.MaThanhToan == thanhToan.MaThanhToan);

            return CreatedAtAction(nameof(GetThanhToan), new { id = saved.MaThanhToan },
                ApiResponse<ThanhToanDto>.Ok(ToDto(saved), "Đã gửi biên lai thành công. Vui lòng đợi chủ trọ xác nhận."));
        }

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINTS MỚI: Danh sách biên lai chờ xác nhận (ChuTro/Admin)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// GET api/ThanhToan/cho-xac-nhan
        /// Chủ trọ / Admin xem danh sách biên lai đang chờ xác nhận.
        /// </summary>
        [HttpGet("cho-xac-nhan")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<ActionResult<IEnumerable<ThanhToanDto>>> GetBienLaiChoXacNhan()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            IQueryable<ThanhToan> query = _context.ThanhToan
                .Include(t => t.HoaDon).ThenInclude(h => h.Phong).ThenInclude(p => p.NhaTro)
                .Include(t => t.NguoiThue)
                .Include(t => t.NguoiXacNhan)
                .Where(t => t.TrangThaiXacNhan == "ChoXacNhan");

            if (role == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(userId);
                query = query.Where(t => maPhongList.Contains(t.HoaDon.MaPhong));
            }

            var list = await query.OrderBy(t => t.NgayThanhToan).ToListAsync();
            return Ok(list.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINTS MỚI: Xác nhận / Từ chối biên lai (ChuTro/Admin)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// PUT api/ThanhToan/{id}/xac-nhan
        /// Chủ trọ / Admin xác nhận hoặc từ chối biên lai.
        /// Body: { chapNhan: true/false, lyDoTuChoi: "..." }
        /// </summary>
        [HttpPut("{id}/xac-nhan")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> XacNhanBienLai(int id, [FromBody] XacNhanBienLaiDto dto)
        {
            var thanhToan = await _context.ThanhToan
                .Include(t => t.HoaDon)
                .Include(t => t.NguoiThue)
                .FirstOrDefaultAsync(t => t.MaThanhToan == id);

            if (thanhToan == null)
                return NotFound(ApiResponse<object>.Loi("Không tìm thấy thanh toán"));

            if (thanhToan.TrangThaiXacNhan != "ChoXacNhan")
                return BadRequest(ApiResponse<object>.Loi("Biên lai này không ở trạng thái chờ xác nhận"));

            // Kiểm tra quyền chủ trọ
            if (GetCurrentRole() == VaiTroConst.ChuTro)
            {
                var maPhongList = await GetMaPhongCuaChuTro(GetCurrentUserId());
                if (!maPhongList.Contains(thanhToan.HoaDon.MaPhong))
                    return Forbid();
            }

            var userId = GetCurrentUserId();

            if (dto.ChapNhan)
            {
                // Xác nhận: kiểm tra số tiền có vượt hóa đơn không
                var tongDaXacNhan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == thanhToan.MaHoaDon
                                && t.MaThanhToan != id
                                && t.TrangThaiXacNhan == "DaXacNhan")
                    .SumAsync(t => t.TongTien);

                var hoaDon = thanhToan.HoaDon;

                // Nếu vượt quá tổng hóa đơn thì vẫn cho phép nhưng trả về cảnh báo
                thanhToan.TrangThaiXacNhan = "DaXacNhan";
                thanhToan.NguoiXacNhanId  = userId;
                thanhToan.NgayXacNhan     = DateTime.Now;
                thanhToan.LyDoTuChoi      = null;

                await _context.SaveChangesAsync();
                await CapNhatTrangThaiHoaDon(thanhToan.MaHoaDon);

                var tongMoi = tongDaXacNhan + thanhToan.TongTien;
                string msg = tongMoi >= hoaDon.TongTien
                    ? "Đã xác nhận thanh toán. Hóa đơn chuyển sang trạng thái Đã thanh toán."
                    : $"Đã xác nhận thanh toán. Hóa đơn thanh toán một phần ({(tongMoi / hoaDon.TongTien * 100):F0}%).";

                return Ok(ApiResponse<object>.Ok(null!, msg));
            }
            else
            {
                // Từ chối
                if (string.IsNullOrWhiteSpace(dto.LyDoTuChoi))
                    return BadRequest(ApiResponse<object>.Loi("Vui lòng nhập lý do từ chối"));

                thanhToan.TrangThaiXacNhan = "TuChoi";
                thanhToan.LyDoTuChoi       = dto.LyDoTuChoi.Trim();
                thanhToan.NguoiXacNhanId   = userId;
                thanhToan.NgayXacNhan      = DateTime.Now;

                await _context.SaveChangesAsync();
                // Không đổi TrangThai HoaDon khi từ chối

                return Ok(ApiResponse<object>.Ok(null!, "Đã từ chối biên lai. Người thuê sẽ được thông báo."));
            }
        }
    }
}
