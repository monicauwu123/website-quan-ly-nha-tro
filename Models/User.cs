using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models
{
    public class User
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(100)]
        public string MatKhau { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(50)]
        public string HoTen { get; set; }

        [StringLength(15)]
        public string SoDienThoai { get; set; }

        [StringLength(20)]
        public string? CCCD { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [StringLength(50)]
        public string? QuocTich { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(100)]
        public string? NoiCongTac { get; set; }

        [StringLength(500)]
        public string? AnhCccdMatTruoc { get; set; }

        [StringLength(500)]
        public string? AnhCccdMatSau { get; set; }

        [StringLength(100)]
        public string? TenNganHang { get; set; }

        [StringLength(50)]
        public string? MaNganHang { get; set; }

        [StringLength(50)]
        public string? SoTaiKhoan { get; set; }

        [StringLength(100)]
        public string? TenChuTaiKhoan { get; set; }

        [StringLength(255)]
        public string? NoiDungChuyenKhoanMacDinh { get; set; }

        /// <summary>Admin | ChuTro | NguoiDung</summary>
        public string VaiTro { get; set; } = "NguoiDung";

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // ─── Reset mật khẩu ──────────────────────────────────────────────────
        [StringLength(200)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────
        public virtual ICollection<NhaTro> DanhSachNhaTro { get; set; }
        public virtual ICollection<NguoiThue> DanhSachNguoiThue { get; set; }
    }
}
