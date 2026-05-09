using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }
        
        [Required]
        public int MaNhaTro { get; set; }
        [ForeignKey("MaNhaTro")]
        public NhaTro NhaTro { get; set; }

        [Required]
        public int MaLoaiPhong { get; set; }
        [ForeignKey("MaLoaiPhong")]
        public LoaiPhong LoaiPhong { get; set; }

        [Required]
        public int MaTrangThai { get; set; }
        [ForeignKey("MaTrangThai")]
        public TrangThai TrangThai { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenPhong { get; set; }

        public float? DienTich { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaPhong { get; set; }

        [Required]
        public int SucChua { get; set; }

        [MaxLength(255)]
        public string? MoTa { get; set; }

        [MaxLength(255)]
        public string? HinhAnh { get; set; }

        /// <summary>JSON array các URL ảnh của phòng.</summary>
        public string? DanhSachHinhAnh { get; set; }

        /// <summary>JSON array MaDichVu được gắn để hiển thị tiện ích/tiện nghi của phòng.</summary>
        public string? DichVuGanPhong { get; set; }

        [MaxLength(255)]
        public string? DiaChiPhong { get; set; }
     
    }
} 
