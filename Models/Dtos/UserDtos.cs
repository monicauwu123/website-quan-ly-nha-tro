using System.ComponentModel.DataAnnotations;
using DoAnSE104.Helpers;

namespace DoAnSE104.Models.Dtos
{
    public class DangKyDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [StringLength(50)]
        public string? HoTen { get; set; }

        [StringLength(15)]
        public string? SoDienThoai { get; set; }

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

        // ─── Thông tin ngân hàng (chỉ dùng khi VaiTro = ChuTro) ──────────────
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
        public string VaiTro { get; set; } = VaiTroConst.NguoiDung;
    }

    public class DangNhapDto
    {
        [Required(ErrorMessage = "Tên đăng nhập hoặc email không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string MatKhau { get; set; }

        /// <summary>Vai trò khi đăng nhập.</summary>
        public string? VaiTro { get; set; }
    }

    public class NguoiDungResponseDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; }
        public string Email { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? CCCD { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? QuocTich { get; set; }
        public string? DiaChi { get; set; }
        public string? NoiCongTac { get; set; }
        public string? AnhCccdMatTruoc { get; set; }
        public string? AnhCccdMatSau { get; set; }
        public string VaiTro { get; set; }
        public string Token { get; set; }
    }
}
