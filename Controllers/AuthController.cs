using Microsoft.AspNetCore.Mvc;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services;
using DoAnSE104.Helpers;

namespace DoAnSE104.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
        /// Đăng nhập — phải chọn vai trò (Admin / ChuTro / NguoiDung)
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
