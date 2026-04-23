using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class YeuCauThue
    {
        [Key]
        public int MaYeuCau { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual User NguoiDung { get; set; }

        [Required]
        public int MaPhong { get; set; }

        [ForeignKey("MaPhong")]
        public virtual Phong Phong { get; set; }

        [Required]
        public DateTime NgayGui { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(30)]
        public string TrangThai { get; set; } = "ChoDuyet";

        [MaxLength(255)]
        public string? GhiChuNguoiDung { get; set; }

        [MaxLength(255)]
        public string? GhiChuChuTro { get; set; }

        public int? MaNguoiThue { get; set; }

        [ForeignKey("MaNguoiThue")]
        public virtual NguoiThue? NguoiThue { get; set; }

        public int? MaHopDong { get; set; }

        [ForeignKey("MaHopDong")]
        public virtual HopDong? HopDong { get; set; }

        public DateTime? NgayXuLy { get; set; }
    }
}
