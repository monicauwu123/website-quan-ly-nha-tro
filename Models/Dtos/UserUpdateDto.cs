using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Há» tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng")]
        [StringLength(100, ErrorMessage = "Há» tÃªn khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100 kÃ½ tá»±")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "TÃªn Ä‘Äƒng nháº­p khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng")]
        [StringLength(50, ErrorMessage = "TÃªn Ä‘Äƒng nháº­p khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 50 kÃ½ tá»±")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Email khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng")]
        [EmailAddress(ErrorMessage = "Email khÃ´ng há»£p lá»‡")]
        [StringLength(100, ErrorMessage = "Email khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100 kÃ½ tá»±")]
        public string Email { get; set; }

        [StringLength(15, ErrorMessage = "Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 15 kÃ½ tá»±")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vai trÃ² khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng")]
        public string VaiTro { get; set; }

        public bool TrangThai { get; set; }
    }
} 
