using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class PhongUpdateDto
    {
    
    [Required]
        public int MaPhong { get; set; }

        [Required]
        public int MaNhaTro { get; set; }

        [Required]
        public int MaLoaiPhong { get; set; }

        [Required]
        public int MaTrangThai { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenPhong { get; set; }

        public float? DienTich { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GiaPhong { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int SucChua { get; set; }

        [MaxLength(255)]
        public string? MoTa { get; set; }

        [MaxLength(255)]
        public string? HinhAnh { get; set; }

        [MaxLength(255)]
        public string? DiaChiPhong { get; set; }
    }
}

