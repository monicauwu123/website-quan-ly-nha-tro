using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        [Required]
        public int MaHoaDon { get; set; }
        [ForeignKey("MaHoaDon")]
        public HoaDon HoaDon { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        [ForeignKey("MaNguoiThue")]
        public NguoiThue NguoiThue { get; set; }

        [Required]
        public DateTime NgayThanhToan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Required]
        [MaxLength(100)]
        public string HinhThucThanhToan { get; set; }

        [MaxLength(255)]
        public string? GhiChu { get; set; }
    }
} 
