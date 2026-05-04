using DoAnSE104.Helpers;
using DoAnSE104.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
    public class HeThongController : ControllerBase
    {
        private readonly IRentalPeriodResetService _rentalPeriodResetService;

        public HeThongController(IRentalPeriodResetService rentalPeriodResetService)
        {
            _rentalPeriodResetService = rentalPeriodResetService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        [HttpPost("ResetKyThue")]
        public async Task<IActionResult> ResetKyThue()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var result = role == VaiTroConst.Admin
                ? await _rentalPeriodResetService.ChotKyThueAsync()
                : await _rentalPeriodResetService.ChotKyThueAsync(userId);

            return Ok(ApiResponse<RentalPeriodResetResult>.Ok(result, "Đã kiểm tra và chốt/reset kỳ thuê tháng"));
        }
    }
}
