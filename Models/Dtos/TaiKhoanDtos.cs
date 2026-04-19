using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    // ─── Tài khoản của tôi ────────────────────────────────────────────────────

    /// <summary>Thông tin hiển thị tài khoản (đọc)</summary>
    public class ThongTinTaiKhoanDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public string VaiTro { get; set; }
        public DateTime NgayTao { get; set; }
        public bool TrangThai { get; set; }
    }

    /// <summary>Cập nhật thông tin tài khoản (chỉ cho sửa HoTen, Email, SoDienThoai)</summary>
    public class CapNhatThongTinDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(50, ErrorMessage = "Họ tên không được vượt quá 50 ký tự")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string? SoDienThoai { get; set; }
    }

    // ─── Đổi mật khẩu ────────────────────────────────────────────────────────

    public class DoiMatKhauDto
    {
        [Required(ErrorMessage = "Mật khẩu cũ không được để trống")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        [StringLength(100)]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string NhapLaiMatKhau { get; set; }
    }

    // ─── Quên mật khẩu ───────────────────────────────────────────────────────

    public class QuenMatKhauDto
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }

    // ─── Reset mật khẩu ──────────────────────────────────────────────────────

    public class ResetMatKhauDto
    {
        [Required(ErrorMessage = "Token không được để trống")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        [StringLength(100)]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string NhapLaiMatKhau { get; set; }
    }
}
