using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class ChiSoDien
    {
        [Key]
        public int MaDien { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public int SoDienCu { get; set; }

        [Required]
        public int SoDienMoi { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaDien { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienDien { get; set; }

        [Required]
        public DateTime NgayThangDien { get; set; }
    }
} 
