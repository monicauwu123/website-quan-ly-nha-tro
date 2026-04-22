using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Helpers;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PhongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public PhongController(ApplicationDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

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

        private async Task<string?> ValidatePhong(Phong phong)
        {
            if (phong.GiaPhong < 0)
                return "Giá phòng phải lớn hơn hoặc bằng 0";

            if (phong.DienTich.HasValue && phong.DienTich.Value < 0)
                return "Diện tích phải lớn hơn hoặc bằng 0";

            if (!await _context.NhaTro.AnyAsync(n => n.MaNhaTro == phong.MaNhaTro))
                return "Nhà trọ không tồn tại";

            if (!await _context.LoaiPhong.AnyAsync(l => l.MaLoaiPhong == phong.MaLoaiPhong))
                return "Loại phòng không tồn tại";

            if (!await _context.TrangThai.AnyAsync(t => t.MaTrangThai == phong.MaTrangThai))
                return "Trạng thái không tồn tại";

            return null;
        }

        // GET: api/Phong
        [HttpGet]
        public async Task<IActionResult> GetPhong()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<Phong> query = _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai);

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    var maPhongList = await _context.NguoiThue
                        .Where(nt => nt.MaNguoiDung == userId)
                        .Select(nt => nt.MaPhong)
                        .ToListAsync();
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhong(int id)
        {
            try
            {
                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .FirstOrDefaultAsync(p => p.MaPhong == id);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    var coQuyen = await _context.NguoiThue
                        .AnyAsync(nt => nt.MaNguoiDung == userId && nt.MaPhong == id);
                    if (!coQuyen) return Forbid();
                }

                return Ok(ApiResponse<Phong>.Ok(phong));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/NhaTro/5
        [HttpGet("NhaTro/{nhaTroId}")]
        public async Task<IActionResult> GetPhongByNhaTro(int nhaTroId)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                // ChuTro kiểm tra nhà trọ có phải của mình không
                if (role == VaiTroConst.ChuTro)
                {
                    var nhaTro = await _context.NhaTro.FindAsync(nhaTroId);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                var data = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .Where(p => p.MaNhaTro == nhaTroId)
                    .ToListAsync();

                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/TrangThai/5
        [HttpGet("TrangThai/{trangThaiId}")]
        public async Task<IActionResult> GetPhongByTrangThai(int trangThaiId)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<Phong> query = _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .Where(p => p.MaTrangThai == trangThaiId);

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    var maPhongList = await _context.NguoiThue
                        .Where(nt => nt.MaNguoiDung == userId)
                        .Select(nt => nt.MaPhong)
                        .ToListAsync();
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/Phong
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostPhong([FromBody] Phong phong)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                // ChuTro chỉ được tạo phòng thuộc nhà trọ của mình
                if (role == VaiTroConst.ChuTro)
                {
                    var nhaTro = await _context.NhaTro.FindAsync(phong.MaNhaTro);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                var loiValidation = await ValidatePhong(phong);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var phongMoi = new Phong
                {
                    MaNhaTro = phong.MaNhaTro,
                    MaLoaiPhong = phong.MaLoaiPhong,
                    MaTrangThai = phong.MaTrangThai,
                    TenPhong = phong.TenPhong,
                    DienTich = phong.DienTich,
                    GiaPhong = phong.GiaPhong,
                    SucChua = phong.SucChua,
                    SoNguoiHienTai = phong.SoNguoiHienTai,
                    MoTa = phong.MoTa,
                    HinhAnh = phong.HinhAnh,
                    DiaChiPhong = phong.DiaChiPhong
                };

                _context.Phong.Add(phongMoi);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPhong), new { id = phongMoi.MaPhong },
                    ApiResponse<Phong>.Ok(phongMoi, "Tạo phòng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/Phong/UploadImage
        // POST: api/Phong/upload-image
        [HttpPost("UploadImage")]
        [HttpPost("upload-image")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn file ảnh"));

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(ApiResponse<object>.Loi("Chỉ chấp nhận: .jpg, .jpeg, .png, .gif"));

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(ApiResponse<object>.Loi("Kích thước file không được vượt quá 5MB"));

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = "phong_images",
                    Transformation = new Transformation().Width(800).Height(600).Crop("fill").Quality("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    var data = new { url = uploadResult.SecureUrl.AbsoluteUri, publicId = uploadResult.PublicId };
                    return Ok(ApiResponse<object>.Ok(data, "Upload ảnh thành công"));
                }

                var cloudinaryError = uploadResult.Error?.Message;
                return StatusCode(500, ApiResponse<object>.Loi(
                    string.IsNullOrWhiteSpace(cloudinaryError)
                        ? "Lỗi upload ảnh lên Cloudinary"
                        : $"Lỗi upload ảnh lên Cloudinary: {cloudinaryError}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi($"Lỗi xử lý ảnh: {ex.Message}"));
            }
        }

        // PUT: api/Phong/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutPhong(int id, [FromBody] Phong phong)
        {
            try
            {
                if (id != phong.MaPhong)
                    return BadRequest(ApiResponse<object>.Loi("Mã phòng không khớp với tham số URL"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();

                    var nhaTro = await _context.NhaTro.FindAsync(phong.MaNhaTro);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                var loiValidation = await ValidatePhong(phong);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                _context.Entry(phong).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Phong>.Ok(phong, "Cập nhật thành công"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Phong.Any(e => e.MaPhong == id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng cần cập nhật"));
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/Phong/5
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeletePhong(int id)
        {
            try
            {
                var phong = await _context.Phong.FindAsync(id);
                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng cần xóa"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();
                }

                _context.Phong.Remove(phong);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null!, "Xóa phòng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
