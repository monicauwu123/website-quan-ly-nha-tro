using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống")]
        public string VaiTro { get; set; }

        public bool TrangThai { get; set; }
    }
} 
