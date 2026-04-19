using System.ComponentModel.DataAnnotations;

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
    }
} 
