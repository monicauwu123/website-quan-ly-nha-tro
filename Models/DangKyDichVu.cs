using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class DangKyDichVu
    {
        [Key]
        public int MaDangKyDichVu { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public User NguoiDung { get; set; }

        [Required]
        public int MaPhong { get; set; }

        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public int MaDichVu { get; set; }

        [ForeignKey("MaDichVu")]
        public DichVu DichVu { get; set; }

        public int? MaNguoiThue { get; set; }

        [ForeignKey("MaNguoiThue")]
        public NguoiThue? NguoiThue { get; set; }

        [Required]
        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        public DateTime? NgayHuy { get; set; }

        /// <summary>
        /// Ngày hệ thống tự chuyển đăng ký sang hết hạn khi bước sang kỳ thuê mới.
        /// Dùng để giữ lịch sử, không xóa dữ liệu đăng ký cũ.
        /// </summary>
        public DateTime? NgayHetHan { get; set; }

        [MaxLength(7)]
        public string? KyDangKy { get; set; }

        [Required]
        [MaxLength(30)]
        public string TrangThai { get; set; } = "DangSuDung";

        [MaxLength(500)]
        public string? GhiChu { get; set; }
    }
}
