using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenDichVu { get; set; }

        [Range(0, 9999999999999, ErrorMessage = "Giá dịch vụ phải là số hợp lệ")]
        public float Tiendichvu { get; set; }

        /// <summary>Chủ trọ sở hữu dịch vụ này. Null chỉ dùng cho dữ liệu cũ/admin.</summary>
        public int? MaChuTro { get; set; }

        [ForeignKey("MaChuTro")]
        public virtual User? ChuTro { get; set; }
    }
}
