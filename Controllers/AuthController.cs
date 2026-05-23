using Microsoft.AspNetCore.Mvc;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services;
using DoAnSE104.Helpers;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace DoAnSE104.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly Cloudinary _cloudinary;

        public AuthController(IAuthService authService, Cloudinary cloudinary)
        {
            _authService = authService;
            _cloudinary = cloudinary;
        }

        /// <summary>
        /// Đăng ký tài khoản mới (ChuTro hoặc NguoiDung)
        /// </summary>
        [HttpPost("dang-ky")]
        public async Task<IActionResult> DangKy([FromBody] DangKyDto dto)
        {
            try
            {
                var ketQua = await _authService.DangKy(dto);
                return Ok(ApiResponse<NguoiDungResponseDto>.Ok(ketQua, "Đăng ký thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }


        /// <summary>
        /// Upload ảnh CCCD khi đăng ký tài khoản người dùng.
        /// </summary>
        [HttpPost("upload-cccd-image")]
        [Consumes("multipart/form-data")]
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

        /// <summary>
        /// Đăng nhập bằng tên đăng nhập hoặc email, kèm vai trò đã chọn.
        /// </summary>
        [HttpPost("dang-nhap")]
        public async Task<IActionResult> DangNhap([FromBody] DangNhapDto dto)
        {
            try
            {
                var ketQua = await _authService.DangNhap(dto);
                return Ok(ApiResponse<NguoiDungResponseDto>.Ok(ketQua, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return Unauthorized(ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
