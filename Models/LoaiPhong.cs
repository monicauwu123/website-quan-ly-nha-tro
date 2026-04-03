using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models
{
    public class LoaiPhong
    {
        [Key]
        public int MaLoaiPhong { get; set; }
        [Required]
        [MaxLength(100)]
        public string TenLoaiPhong { get; set; }
        [MaxLength(255)]
        public string? MoTa { get; set; }
    }
} 
