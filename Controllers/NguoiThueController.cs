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
    public class NguoiThueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IDeleteValidationService _deleteValidationService;

        public NguoiThueController(ApplicationDbContext context, Cloudinary cloudinary, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _cloudinary = cloudinary;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<bool> PhongThuocChuTro(int maPhong, int maChuTro)
        {
            return await _context.Phong
                .Include(p => p.NhaTro)
                .AnyAsync(p => p.MaPhong == maPhong && p.NhaTro.MaChuTro == maChuTro);
        }

        private void DongBoThongTinHienThiTuTaiKhoan(NguoiThue nguoiThue, User? user)
        {
            if (user == null) return;

            // Thông tin cá nhân/CCCD lấy theo tài khoản mới nhất để chủ trọ luôn thấy dữ liệu người dùng vừa cập nhật.
            nguoiThue.HoTen = string.IsNullOrWhiteSpace(user.HoTen) ? nguoiThue.HoTen : user.HoTen;
            nguoiThue.CCCD = user.CCCD ?? nguoiThue.CCCD;
            nguoiThue.SDT = string.IsNullOrWhiteSpace(user.SoDienThoai) ? nguoiThue.SDT : user.SoDienThoai;
            nguoiThue.Email = string.IsNullOrWhiteSpace(user.Email) ? nguoiThue.Email : user.Email;
            nguoiThue.NgaySinh = user.NgaySinh ?? nguoiThue.NgaySinh;
            nguoiThue.GioiTinh = user.GioiTinh ?? nguoiThue.GioiTinh;
            nguoiThue.QuocTich = user.QuocTich ?? nguoiThue.QuocTich;
            nguoiThue.DiaChi = user.DiaChi ?? nguoiThue.DiaChi;
            nguoiThue.NoiCongTac = user.NoiCongTac ?? nguoiThue.NoiCongTac;
            nguoiThue.AnhCccdMatTruoc = user.AnhCccdMatTruoc ?? nguoiThue.AnhCccdMatTruoc;
            nguoiThue.AnhCccdMatSau = user.AnhCccdMatSau ?? nguoiThue.AnhCccdMatSau;
        }

        private async Task DongBoDanhSachHienThiTuTaiKhoan(List<NguoiThue> danhSach)
        {
            var userIds = danhSach
                .Where(nt => nt.MaNguoiDung.HasValue)
                .Select(nt => nt.MaNguoiDung!.Value)
                .Distinct()
                .ToList();

            var hoSoChuaLienKet = danhSach
                .Where(nt => !nt.MaNguoiDung.HasValue)
                .Select(nt => nt.MaNguoiThue)
                .Distinct()
                .ToList();

            var userIdTheoHoSo = new Dictionary<int, int>();
            if (hoSoChuaLienKet.Any())
            {
                userIdTheoHoSo = await _context.YeuCauThue
                    .Where(y => y.MaNguoiThue.HasValue && hoSoChuaLienKet.Contains(y.MaNguoiThue.Value))
                    .GroupBy(y => y.MaNguoiThue!.Value)
                    .Select(g => new { MaNguoiThue = g.Key, MaNguoiDung = g.OrderByDescending(y => y.NgayXuLy ?? y.NgayGui).Select(y => y.MaNguoiDung).FirstOrDefault() })
                    .ToDictionaryAsync(x => x.MaNguoiThue, x => x.MaNguoiDung);

                userIds.AddRange(userIdTheoHoSo.Values);
                userIds = userIds.Distinct().ToList();
            }

            if (!userIds.Any()) return;

            var users = await _context.Users
                .Where(u => userIds.Contains(u.MaNguoiDung))
                .ToDictionaryAsync(u => u.MaNguoiDung);

            foreach (var nt in danhSach)
            {
                var maNguoiDung = nt.MaNguoiDung;
                if (!maNguoiDung.HasValue && userIdTheoHoSo.TryGetValue(nt.MaNguoiThue, out var maNguoiDungTuYeuCau))
                {
                    maNguoiDung = maNguoiDungTuYeuCau;
                    nt.MaNguoiDung = maNguoiDungTuYeuCau;
                }

                if (maNguoiDung.HasValue && users.TryGetValue(maNguoiDung.Value, out var user))
                    DongBoThongTinHienThiTuTaiKhoan(nt, user);
            }
        }

        private async Task<User?> TimTaiKhoanLienKet(NguoiThue nguoiThue)
        {
            if (nguoiThue.MaNguoiDung.HasValue)
                return await _context.Users.FindAsync(nguoiThue.MaNguoiDung.Value);

            var maNguoiDungTuYeuCau = await _context.YeuCauThue
                .Where(y => y.MaNguoiThue == nguoiThue.MaNguoiThue)
                .OrderByDescending(y => y.NgayXuLy ?? y.NgayGui)
                .Select(y => (int?)y.MaNguoiDung)
                .FirstOrDefaultAsync();

            if (maNguoiDungTuYeuCau.HasValue)
            {
                nguoiThue.MaNguoiDung = maNguoiDungTuYeuCau.Value;
                return await _context.Users.FindAsync(maNguoiDungTuYeuCau.Value);
            }

            return await _context.Users.FirstOrDefaultAsync(u =>
                (!string.IsNullOrWhiteSpace(nguoiThue.Email) && u.Email == nguoiThue.Email) ||
                (!string.IsNullOrWhiteSpace(nguoiThue.SDT) && u.SoDienThoai == nguoiThue.SDT) ||
                (!string.IsNullOrWhiteSpace(nguoiThue.CCCD) && u.CCCD == nguoiThue.CCCD));
        }

        private async Task<string?> ValidateNguoiThue(NguoiThue nguoiThue)
        {
            if (string.IsNullOrWhiteSpace(nguoiThue.HoTen))
                return "Họ tên người thuê không được để trống";

            if (!await _context.Phong.AnyAsync(p => p.MaPhong == nguoiThue.MaPhong))
                return "Phòng không tồn tại";

            if (nguoiThue.MaNguoiDung.HasValue && !await _context.Users.AnyAsync(u => u.MaNguoiDung == nguoiThue.MaNguoiDung.Value))
                return "Tài khoản người dùng liên kết không tồn tại";

            return null;
        }

        // GET: api/NguoiThue
        [HttpGet]
        public async Task<IActionResult> GetNguoiThue()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<NguoiThue> query = _context.NguoiThue.AsNoTracking();

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await _context.Phong
                        .Where(p => p.NhaTro.MaChuTro == userId)
                        .Select(p => p.MaPhong)
                        .ToListAsync();

                    // Lấy cả hồ sơ đang gắn MaPhong và hồ sơ có hợp đồng thuộc phòng của chủ trọ.
                    // Cách này tránh bị mất khách thuê nếu dữ liệu cũ chỉ còn liên kết qua hợp đồng.
                    query = query.Where(nt =>
                        maPhongList.Contains(nt.MaPhong) ||
                        _context.HopDong.Any(h => h.MaNguoiThue == nt.MaNguoiThue && maPhongList.Contains(h.MaPhong)));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    query = query.Where(nt => nt.MaNguoiDung == userId);
                }

                var data = await query.ToListAsync();
                await DongBoDanhSachHienThiTuTaiKhoan(data);
                return Ok(ApiResponse<List<NguoiThue>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NguoiThue/cua-toi
        [HttpGet("cua-toi")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> GetNguoiThueCuaToi()
        {
            try
            {
                var userId = GetCurrentUserId();

                var data = await (
                    from nt in _context.NguoiThue
                    join p in _context.Phong on nt.MaPhong equals p.MaPhong
                    join n in _context.NhaTro on p.MaNhaTro equals n.MaNhaTro
                    where nt.MaNguoiDung == userId
                    select new
                    {
                        nt.MaNguoiThue,
                        nt.HoTen,
                        nt.CCCD,
                        nt.SDT,
                        nt.Email,
                        nt.NgaySinh,
                        nt.GioiTinh,
                        nt.QuocTich,
                        nt.DiaChi,
                        nt.NoiCongTac,
                        nt.MaPhong,
                        nt.MaNguoiDung,
                        nt.AnhCccdMatTruoc,
                        nt.AnhCccdMatSau,
                        p.TenPhong,
                        n.TenNhaTro
                    }).ToListAsync();

                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NguoiThue/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue.FindAsync(id);
                if (nguoiThue == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.NguoiDung && nguoiThue.MaNguoiDung != userId)
                    return Forbid();

                if (role == VaiTroConst.ChuTro)
                {
                    if (!await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                        return Forbid();
                }

                var linkedUser = await TimTaiKhoanLienKet(nguoiThue);
                DongBoThongTinHienThiTuTaiKhoan(nguoiThue, linkedUser);

                return Ok(ApiResponse<NguoiThue>.Ok(nguoiThue));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NguoiThue/Search
        [HttpGet("Search")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> SearchNguoiThue(string? keyword)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<NguoiThue> query = _context.NguoiThue.AsNoTracking();

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await _context.Phong
                        .Where(p => p.NhaTro.MaChuTro == userId)
                        .Select(p => p.MaPhong)
                        .ToListAsync();

                    query = query.Where(nt =>
                        maPhongList.Contains(nt.MaPhong) ||
                        _context.HopDong.Any(h => h.MaNguoiThue == nt.MaNguoiThue && maPhongList.Contains(h.MaPhong)));
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(n =>
                        n.HoTen.Contains(keyword) ||
                        (n.CCCD != null && n.CCCD.Contains(keyword)) ||
                        (n.SDT != null && n.SDT.Contains(keyword)) ||
                        (n.Email != null && n.Email.Contains(keyword)));
                }

                var data = await query.ToListAsync();
                await DongBoDanhSachHienThiTuTaiKhoan(data);
                return Ok(ApiResponse<List<NguoiThue>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/NguoiThue/upload-cccd-image
        [HttpPost("upload-cccd-image")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro},{VaiTroConst.NguoiDung}")]
        public async Task<IActionResult> UploadCccdImage([FromForm] UploadCccdImageDto dto)
        {
            try
            {
                var file = dto?.File;

                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn file ảnh CCCD"));

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(ApiResponse<object>.Loi("Chỉ chấp nhận ảnh: .jpg, .jpeg, .png, .gif, .webp"));

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(ApiResponse<object>.Loi("Kích thước ảnh CCCD không được vượt quá 5MB"));

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = "nguoi_thue_cccd",
                    Transformation = new Transformation().Width(1200).Height(800).Crop("limit").Quality("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    var data = new { url = uploadResult.SecureUrl.AbsoluteUri, publicId = uploadResult.PublicId };
                    return Ok(ApiResponse<object>.Ok(data, "Upload ảnh CCCD thành công"));
                }

                var cloudinaryError = uploadResult.Error?.Message;
                return StatusCode(500, ApiResponse<object>.Loi(
                    string.IsNullOrWhiteSpace(cloudinaryError)
                        ? "Lỗi upload ảnh CCCD lên Cloudinary"
                        : $"Lỗi upload ảnh CCCD lên Cloudinary: {cloudinaryError}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi($"Lỗi xử lý ảnh CCCD: {ex.Message}"));
            }
        }

        // POST: api/NguoiThue
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostNguoiThue([FromBody] NguoiThue nguoiThue)
        {
            try
            {
                var loiValidation = await ValidateNguoiThue(nguoiThue);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro && !await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                    return Forbid();

                if (!nguoiThue.MaNguoiDung.HasValue)
                {
                    var linkedUser = await _context.Users.FirstOrDefaultAsync(u =>
                        (!string.IsNullOrWhiteSpace(nguoiThue.Email) && u.Email == nguoiThue.Email) ||
                        (!string.IsNullOrWhiteSpace(nguoiThue.SDT) && u.SoDienThoai == nguoiThue.SDT));

                    if (linkedUser != null)
                        nguoiThue.MaNguoiDung = linkedUser.MaNguoiDung;
                }

                _context.NguoiThue.Add(nguoiThue);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNguoiThue), new { id = nguoiThue.MaNguoiThue },
                    ApiResponse<NguoiThue>.Ok(nguoiThue, "Thêm người thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/NguoiThue/cua-toi/5
        [HttpPut("cua-toi/{id}")]
        [Authorize(Roles = VaiTroConst.NguoiDung)]
        public async Task<IActionResult> CapNhatNguoiThueCuaToi(int id, [FromBody] NguoiThueSelfUpdateDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiThue == id && nt.MaNguoiDung == userId);

                if (nguoiThue == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy thông tin khách thuê thuộc tài khoản của bạn"));

                nguoiThue.HoTen = dto.HoTen.Trim();
                nguoiThue.CCCD = dto.CCCD;
                nguoiThue.SDT = dto.SDT;
                nguoiThue.Email = dto.Email;
                nguoiThue.NgaySinh = dto.NgaySinh;
                nguoiThue.GioiTinh = dto.GioiTinh;
                nguoiThue.QuocTich = dto.QuocTich;
                nguoiThue.DiaChi = dto.DiaChi;
                nguoiThue.NoiCongTac = dto.NoiCongTac;
                nguoiThue.AnhCccdMatTruoc = dto.AnhCccdMatTruoc;
                nguoiThue.AnhCccdMatSau = dto.AnhCccdMatSau;

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.HoTen = nguoiThue.HoTen;
                    user.CCCD = nguoiThue.CCCD;
                    user.SoDienThoai = nguoiThue.SDT ?? string.Empty;
                    user.Email = nguoiThue.Email ?? user.Email;
                    user.NgaySinh = nguoiThue.NgaySinh;
                    user.GioiTinh = nguoiThue.GioiTinh;
                    user.QuocTich = nguoiThue.QuocTich;
                    user.DiaChi = nguoiThue.DiaChi;
                    user.NoiCongTac = nguoiThue.NoiCongTac;
                    user.AnhCccdMatTruoc = nguoiThue.AnhCccdMatTruoc;
                    user.AnhCccdMatSau = nguoiThue.AnhCccdMatSau;

                    var cacHoSoKhac = await _context.NguoiThue
                        .Where(nt => nt.MaNguoiDung == userId && nt.MaNguoiThue != id)
                        .ToListAsync();

                    foreach (var nt in cacHoSoKhac)
                    {
                        nt.HoTen = nguoiThue.HoTen;
                        nt.CCCD = nguoiThue.CCCD;
                        nt.SDT = nguoiThue.SDT;
                        nt.Email = nguoiThue.Email;
                        nt.NgaySinh = nguoiThue.NgaySinh;
                        nt.GioiTinh = nguoiThue.GioiTinh;
                        nt.QuocTich = nguoiThue.QuocTich;
                        nt.DiaChi = nguoiThue.DiaChi;
                        nt.NoiCongTac = nguoiThue.NoiCongTac;
                        nt.AnhCccdMatTruoc = nguoiThue.AnhCccdMatTruoc;
                        nt.AnhCccdMatSau = nguoiThue.AnhCccdMatSau;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<NguoiThue>.Ok(nguoiThue, "Cập nhật thông tin khách thuê thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/NguoiThue/5
        [HttpPut("{id}")]
        [Authorize(Roles = VaiTroConst.Admin)]
        public async Task<IActionResult> PutNguoiThue(int id, [FromBody] NguoiThue nguoiThue)
        {
            try
            {
                if (id != nguoiThue.MaNguoiThue)
                    return BadRequest(ApiResponse<object>.Loi("Mã người thuê không khớp"));

                var existing = await _context.NguoiThue.AsNoTracking().FirstOrDefaultAsync(nt => nt.MaNguoiThue == id);
                if (existing == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var loiValidation = await ValidateNguoiThue(nguoiThue);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                _context.Entry(nguoiThue).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<NguoiThue>.Ok(nguoiThue, "Cập nhật thành công"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.NguoiThue.Any(e => e.MaNguoiThue == id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/NguoiThue/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue.FindAsync(id);
                if (nguoiThue == null || nguoiThue.TrangThai == "DaXoa")
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy người thuê"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro && !await PhongThuocChuTro(nguoiThue.MaPhong, userId))
                    return Forbid();

                // Kiểm tra dữ liệu liên quan
                var result = await _deleteValidationService.DeleteNguoiThueAsync(id);
                return this.ToActionResult(result);

                var coHopDong    = await _context.HopDong.AnyAsync(h => h.MaNguoiThue == id);
                var coHoaDon     = await _context.HoaDon.AnyAsync(hd => hd.MaNguoiThue == id);
                var coThanhToan  = await _context.ThanhToan.AnyAsync(tt => tt.MaNguoiThue == id);
                var coYeuCauThue = await _context.YeuCauThue.AnyAsync(y => y.MaNguoiThue == id);

                if (!coHopDong && !coHoaDon && !coThanhToan && !coYeuCauThue)
                {
                    // Chưa phát sinh dữ liệu → Xóa cứng
                    _context.NguoiThue.Remove(nguoiThue);
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(null!, "Đã xóa khách thuê thành công"));
                }
                else
                {
                    // Đã có dữ liệu liên quan → Chuyển trạng thái KhongHoatDong
                    nguoiThue.TrangThai = "KhongHoatDong";
                    await _context.SaveChangesAsync();

                    var lyDo = new List<string>();
                    if (coHopDong)    lyDo.Add("hợp đồng");
                    if (coHoaDon)     lyDo.Add("hóa đơn");
                    if (coThanhToan)  lyDo.Add("thanh toán");
                    if (coYeuCauThue) lyDo.Add("yêu cầu thuê");

                    return Ok(ApiResponse<object>.Ok(null!,
                        $"Khách thuê đã có {string.Join(", ", lyDo)} liên quan. " +
                        "Đã chuyển sang trạng thái \"Không còn hoạt động\"."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
