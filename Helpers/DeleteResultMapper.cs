using DoAnSE104.Services;
using Microsoft.AspNetCore.Mvc;

namespace DoAnSE104.Helpers
{
    public static class DeleteResultMapper
    {
        public static IActionResult ToActionResult(this ControllerBase controller, DeleteResult result)
        {
            if (result.Action == DeleteAction.NotFound)
                return controller.NotFound(ApiResponse<object>.Loi(result.Message));

            if (result.Action == DeleteAction.Blocked)
                return controller.BadRequest(ApiResponse<object>.Loi(result.Message));

            return controller.Ok(ApiResponse<object>.Ok(null!, result.Message));
        }
    }
}
