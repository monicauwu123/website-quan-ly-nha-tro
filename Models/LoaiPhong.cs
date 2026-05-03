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

        /// <summary>Nhà trọ sở hữu loại phòng này.</summary>
        public int? MaNhaTro { get; set; }

        [ForeignKey("MaNhaTro")]
        public virtual NhaTro? NhaTro { get; set; }

        /// <summary>Giữ lại để tương thích dữ liệu cũ. Dữ liệu mới ưu tiên MaNhaTro.</summary>
        public int? MaChuTro { get; set; }

        [ForeignKey("MaChuTro")]
        public virtual User? ChuTro { get; set; }

        /// <summary>
        /// Trạng thái loại phòng: DangSuDung | NgungSuDung | DaXoa
        /// </summary>
        [MaxLength(30)]
        public string TrangThai { get; set; } = "DangSuDung";
    }
}
