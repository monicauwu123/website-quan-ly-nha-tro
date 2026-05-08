using Microsoft.AspNetCore.Http;

namespace DoAnSE104.Models.Dtos
{
    public class UploadImageDto
    {
        public IFormFile? File { get; set; }
    }
}
