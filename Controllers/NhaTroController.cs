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
    public class NhaTroController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IDeleteValidationService _deleteValidationService;

        public NhaTroController(ApplicationDbContext context, Cloudinary cloudinary, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _cloudinary = cloudinary;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private static string? ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Vui lòng chọn file ảnh";

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension) || !allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP";

            if (file.Length > 5 * 1024 * 1024)
                return "Kích thước file không được vượt quá 5MB";

            return null;
        }

        // GET: api/NhaTro
        [HttpGet]
        public async Task<IActionResult> GetNhaTro()
        {
            try
            {
                var role = GetCurrentRole();
                IQueryable<NhaTro> query = _context.NhaTro
                    .Where(n => n.TrangThai != "DaXoa");

                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    query = query.Where(n => n.MaChuTro == userId);
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<NhaTro>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/NhaTro/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNhaTro(int id)
        {
            try
            {
                var nhaTro = await _context.NhaTro.FindAsync(id);
                if (nhaTro == null || nhaTro.TrangThai == "DaXoa")
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro && nhaTro.MaChuTro != userId)
                    return Forbid();

                return Ok(ApiResponse<NhaTro>.Ok(nhaTro));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/NhaTro
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostNhaTro([FromBody] NhaTro nhaTro)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro)
                    nhaTro.MaChuTro = userId;

                nhaTro.TrangThai = "HoatDong";

                _context.NhaTro.Add(nhaTro);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNhaTro), new { id = nhaTro.MaNhaTro },
                    ApiResponse<NhaTro>.Ok(nhaTro, "Tạo nhà trọ thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // PUT: api/NhaTro/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutNhaTro(int id, [FromBody] NhaTro nhaTro)
        {
            try
            {
                if (id != nhaTro.MaNhaTro)
                    return BadRequest(ApiResponse<object>.Loi("Mã nhà trọ không khớp"));

                var existing = await _context.NhaTro.FindAsync(id);
                if (existing == null || existing.TrangThai == "DaXoa")
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro && existing.MaChuTro != GetCurrentUserId())
                    return Forbid();

                existing.TenNhaTro = nhaTro.TenNhaTro;
                existing.DiaChi = nhaTro.DiaChi;
                existing.MoTa = nhaTro.MoTa;
                existing.HinhAnh = nhaTro.HinhAnh;
                existing.DanhSachHinhAnh = nhaTro.DanhSachHinhAnh;
                if (role == VaiTroConst.Admin)
                    existing.MaChuTro = nhaTro.MaChuTro;

                await _context.SaveChangesAsync();
                return Ok(ApiResponse<NhaTro>.Ok(existing, "Cập nhật thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/NhaTro/upload-image
        [HttpPost("UploadImage")]
        [HttpPost("upload-image")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageDto dto)
        {
            try
            {
                var file = dto?.File;
                var validationError = ValidateImageFile(file!);
                if (validationError != null)
                    return BadRequest(ApiResponse<object>.Loi(validationError));

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = "nha_tro_images",
                    Transformation = new Transformation().Width(1200).Height(800).Crop("limit").Quality("auto")
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

        // DELETE: api/NhaTro/5
        // Logic:
        //   Chưa có dữ liệu liên quan → Xóa cứng, báo "Đã xóa thành công"
        //   Đã có dữ liệu liên quan   → Chuyển TrangThai = NgungHoatDong, báo rõ lý do
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeleteNhaTro(int id)
        {
            try
            {
                var nhaTro = await _context.NhaTro.FindAsync(id);
                if (nhaTro == null || nhaTro.TrangThai == "DaXoa")
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy nhà trọ"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro && nhaTro.MaChuTro != GetCurrentUserId())
                    return Forbid();

                var result = await _deleteValidationService.DeleteNhaTroAsync(id);
                return this.ToActionResult(result);

                // Kiểm tra dữ liệu liên quan
                var coPhong      = await _context.Phong.AnyAsync(p => p.MaNhaTro == id);
                var coLoaiPhong  = await _context.LoaiPhong.AnyAsync(lp => lp.MaNhaTro == id);
                var coDichVu     = await _context.DichVu.AnyAsync(dv => dv.MaNhaTro == id);

                if (!coPhong && !coLoaiPhong && !coDichVu)
                {
                    // Chưa phát sinh dữ liệu → Xóa cứng
                    _context.NhaTro.Remove(nhaTro);
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(null!, "Đã xóa nhà trọ thành công"));
                }
                else
                {
                    // Đã có dữ liệu liên quan → Chuyển trạng thái Ngưng hoạt động
                    nhaTro.TrangThai = "NgungHoatDong";
                    await _context.SaveChangesAsync();

                    var lyDo = new List<string>();
                    if (coPhong)     lyDo.Add("phòng");
                    if (coLoaiPhong) lyDo.Add("loại phòng");
                    if (coDichVu)    lyDo.Add("dịch vụ");

                    return Ok(ApiResponse<object>.Ok(null!,
                        $"Nhà trọ đã có {string.Join(", ", lyDo)} liên quan. " +
                        "Đã chuyển sang trạng thái \"Ngưng hoạt động\"."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
