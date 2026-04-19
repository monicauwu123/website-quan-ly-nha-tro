using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class ChiSoNuoc
    {
        [Key]
        public int MaNuoc { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public int SoNuocCu { get; set; }

        [Required]
        public int SoNuocMoi { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaNuoc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienNuoc { get; set; }

        [Required]
        public DateTime NgayThangNuoc { get; set; }
    }
} 
