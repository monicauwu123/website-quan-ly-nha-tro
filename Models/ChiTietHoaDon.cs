using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int MaChiTiet { get; set; }

        [Required]
        public int MaHoaDon { get; set; }
        public HoaDon HoaDon { get; set; }

        // Lo?i kho?n ti?n: v� d? "TienPhong", "TienDien", "TienNuoc", "TienDichVu"
        [Required]
        [MaxLength(50)]
        public string LoaiKhoan { get; set; }

        // S? ti?n c?a kho?n ?�
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }
    }
}

