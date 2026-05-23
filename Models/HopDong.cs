using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class HopDong
    {
        [Key]
        public int MaHopDong { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        [ForeignKey("MaNguoiThue")]
        public NguoiThue NguoiThue { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public DateTime NgayBatDau { get; set; }

        public DateTime? NgayKetThuc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienCoc { get; set; }

        [MaxLength(1000)]
        public string? NoiDung { get; set; }

        /// <summary>
        /// Trạng thái hợp đồng: ChoXacNhan | DangHieuLuc | KetThuc | Huy
        /// </summary>
        [MaxLength(30)]
        public string TrangThai { get; set; } = "DangHieuLuc";
      
    }
} 
