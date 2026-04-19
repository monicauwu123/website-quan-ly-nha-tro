using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class LichSuGiaDichVu
    {
        [Key]
        public int MaLichSu { get; set; }

        [Required]
        public int MaDichVu { get; set; }
        [ForeignKey("MaDichVu")]
        public DichVu DichVu { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaDichVu { get; set; }

        [Required]
        public DateTime NgayHieuLuc { get; set; }
    }
} 
