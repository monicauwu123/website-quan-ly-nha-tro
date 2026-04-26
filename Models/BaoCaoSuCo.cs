using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class BaoCaoSuCo
    {
        [Key]
        public int MaBaoCao { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public User NguoiDung { get; set; }

        [Required]
        public int MaPhong { get; set; }

        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        [MaxLength(150)]
        public string TieuDe { get; set; }

        [Required]
        [MaxLength(1000)]
        public string NoiDung { get; set; }

        [MaxLength(30)]
        public string MucDo { get; set; } = "Bình thường";

        [MaxLength(50)]
        public string TrangThai { get; set; } = "Moi";

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public DateTime? NgayXuLy { get; set; }

        [MaxLength(1000)]
        public string? PhanHoiChuTro { get; set; }
    }
}
