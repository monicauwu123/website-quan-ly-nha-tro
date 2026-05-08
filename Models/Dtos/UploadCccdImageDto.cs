using Microsoft.AspNetCore.Http;

namespace DoAnSE104.Models.Dtos
{
    public class UploadCccdImageDto
    {
        public IFormFile? File { get; set; }
    }
}
