using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Dtos
{
    public class ChiSoNuocDtoCreate
    {
        [Required]
        public int MaPhong { get; set; }

        [Required]
        public int SoNuocCu { get; set; }

        [Required]
        public int SoNuocMoi { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GiaNuoc { get; set; }

        [Required]
        public DateTime NgayThangNuoc { get; set; }
    }
}

