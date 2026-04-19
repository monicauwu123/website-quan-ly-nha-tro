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

        /// <summary>Admin | ChuTro | NguoiDung</summary>
        public string VaiTro { get; set; } = VaiTroConst.NguoiDung;
    }

    public class DangNhapDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string MatKhau { get; set; }

        /// <summary>Admin | ChuTro | NguoiDung — dùng để xác nhận role khi login</summary>
        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public string VaiTro { get; set; }
    }

    public class NguoiDungResponseDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; }
        public string Email { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string VaiTro { get; set; }
        public string Token { get; set; }
    }
}
