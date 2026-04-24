using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>Chủ trọ sở hữu loại phòng này. Null chỉ dùng cho dữ liệu cũ/admin.</summary>
        public int? MaChuTro { get; set; }

        [ForeignKey("MaChuTro")]
        public virtual User? ChuTro { get; set; }
    }
}
