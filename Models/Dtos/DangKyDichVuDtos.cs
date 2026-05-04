using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class DangKyDichVuCreateDto
    {
        [Required]
        public int MaPhong { get; set; }

        [Required]
        public int MaDichVu { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }
    }

    public class DangKyDichVuDto
    {
        public int MaDangKyDichVu { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenNhaTro { get; set; } = string.Empty;
        public int MaDichVu { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public decimal TienDichVu { get; set; }
        public DateTime NgayDangKy { get; set; }
        public DateTime? NgayHuy { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public string? KyDangKy { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string TenTrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public string? TenNguoiDung { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
    }
}
