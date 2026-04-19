using System.Security.Claims;
using DoAnSE104.Helpers;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnSE104.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // ─── Tài khoản của tôi ───────────────────────────────────────────────

        /// <summary>
        /// Lấy thông tin tài khoản đang đăng nhập
        /// </summary>
        [HttpGet("thong-tin")]
        [Authorize]
        public async Task<IActionResult> LayThongTin()
        {
            try
            {
                var maNguoiDung = LayMaNguoiDung();
                var result = await _accountService.LayThongTin(maNguoiDung);
                return Ok(ApiResponse<ThongTinTaiKhoanDto>.Ok(result, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                return NotFound(ApiResponse<object>.Loi(ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản (chỉ HoTen, Email, SoDienThoai)
        /// </summary>
        [HttpPut("cap-nhat")]
        [Authorize]
        public async Task<IActionResult> CapNhatThongTin([FromBody] CapNhatThongTinDto dto)
        {
            try
            {
                var maNguoiDung = LayMaNguoiDung();
                var result = await _accountService.CapNhatThongTin(maNguoiDung, dto);
                return Ok(ApiResponse<ThongTinTaiKhoanDto>.Ok(result, "Cập nhật thông tin thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }

        // ─── Đổi mật khẩu ────────────────────────────────────────────────────

        /// <summary>
        /// Đổi mật khẩu (yêu cầu đăng nhập)
        /// </summary>
        [HttpPost("doi-mat-khau")]
        [Authorize]
        public async Task<IActionResult> DoiMatKhau([FromBody] DoiMatKhauDto dto)
        {
            try
            {
                var maNguoiDung = LayMaNguoiDung();
                await _accountService.DoiMatKhau(maNguoiDung, dto);
                return Ok(ApiResponse<object>.Ok(null!, "Đổi mật khẩu thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }

        // ─── Quên mật khẩu ───────────────────────────────────────────────────

        /// <summary>
        /// Gửi OTP về email để đặt lại mật khẩu (không cần đăng nhập)
        /// </summary>
        [HttpPost("quen-mat-khau")]
        [AllowAnonymous]
        public async Task<IActionResult> QuenMatKhau([FromBody] QuenMatKhauDto dto)
        {
            try
            {
                // Lấy base URL từ request
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                await _accountService.QuenMatKhau(dto.Email, baseUrl);

                // Luôn trả thành công để không lộ email có tồn tại không
                return Ok(ApiResponse<object>.Ok(null!,
                    "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mã OTP. Vui lòng kiểm tra hộp thư."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }

        // ─── Reset mật khẩu ──────────────────────────────────────────────────

        /// <summary>
        /// Đặt lại mật khẩu bằng OTP từ email (không cần đăng nhập)
        /// </summary>
        [HttpPost("reset-mat-khau")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetMatKhau([FromBody] ResetMatKhauDto dto)
        {
            try
            {
                await _accountService.ResetMatKhau(dto);
                return Ok(ApiResponse<object>.Ok(null!, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Loi(ex.Message));
            }
        }

        // ─── Helper ──────────────────────────────────────────────────────────

        private int LayMaNguoiDung()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("MaNguoiDung")?.Value;

            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int id))
                throw new Exception("Không xác định được tài khoản. Vui lòng đăng nhập lại.");

            return id;
        }
    }
}
