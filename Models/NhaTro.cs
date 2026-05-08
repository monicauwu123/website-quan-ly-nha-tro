using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class NhaTro
    {
        [Key]
        public int MaNhaTro { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenNhaTro { get; set; }

        [Required]
        [MaxLength(255)]
        public string DiaChi { get; set; }

        [MaxLength(255)]
        public string? MoTa { get; set; }

        [MaxLength(255)]
        public string? HinhAnh { get; set; }

        /// <summary>JSON array các URL ảnh của nhà trọ.</summary>
        public string? DanhSachHinhAnh { get; set; }

        /// <summary>FK tới User (ChuTro sở hữu nhà trọ này)</summary>
        public int? MaChuTro { get; set; }

        [ForeignKey("MaChuTro")]
        public virtual User? ChuTro { get; set; }

        /// <summary>
        /// Trạng thái nhà trọ: HoatDong | NgungHoatDong | DaXoa
        /// </summary>
        [MaxLength(30)]
        public string TrangThai { get; set; } = "HoatDong";
    }
}
