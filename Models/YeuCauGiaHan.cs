using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class YeuCauGiaHan
    {
        [Key]
        public int MaYeuCauGiaHan { get; set; }

        [Required]
        public int MaHopDong { get; set; }

        [ForeignKey("MaHopDong")]
        public virtual HopDong HopDong { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual User NguoiDung { get; set; }

        [Required]
        public DateTime NgayGui { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(30)]
        public string TrangThai { get; set; } = "ChoDuyet";

        public DateTime? NgayKetThucCu { get; set; }

        [Required]
        public DateTime NgayKetThucMoiDeXuat { get; set; }

        public DateTime? NgayKetThucMoiChuTro { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienCocMoi { get; set; }

        [MaxLength(1000)]
        public string? NoiDungDieuKhoanMoi { get; set; }

        [MaxLength(500)]
        public string? GhiChuNguoiDung { get; set; }

        [MaxLength(500)]
        public string? GhiChuChuTro { get; set; }

        public DateTime? NgayXuLy { get; set; }
    }
}
