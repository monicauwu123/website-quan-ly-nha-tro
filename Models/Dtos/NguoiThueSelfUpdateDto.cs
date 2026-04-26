using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class NguoiThueSelfUpdateDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "CCCD/CMND không được vượt quá 20 ký tự")]
        public string? CCCD { get; set; }

        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string? SDT { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? Email { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(10, ErrorMessage = "Giới tính không được vượt quá 10 ký tự")]
        public string? GioiTinh { get; set; }

        [StringLength(50, ErrorMessage = "Quốc tịch không được vượt quá 50 ký tự")]
        public string? QuocTich { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string? DiaChi { get; set; }

        [StringLength(100, ErrorMessage = "Nơi công tác không được vượt quá 100 ký tự")]
        public string? NoiCongTac { get; set; }

        [StringLength(500)]
        public string? AnhCccdMatTruoc { get; set; }

        [StringLength(500)]
        public string? AnhCccdMatSau { get; set; }
    }
}
