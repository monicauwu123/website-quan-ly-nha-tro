using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class HoaDonUpdateDto : HoaDonCreateDto
    {
        [Required]
        public int MaHoaDon { get; set; }
    }
}
