using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class TaoBaoCaoSuCoDto
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng cần báo cáo")]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự")]
        public string TieuDe { get; set; }

        [Required(ErrorMessage = "Nội dung sự cố không được để trống")]
        [MaxLength(1000, ErrorMessage = "Nội dung sự cố không được vượt quá 1000 ký tự")]
        public string NoiDung { get; set; }

        [MaxLength(30)]
        public string? MucDo { get; set; }
    }

    public class XuLyBaoCaoSuCoDto
    {
        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [MaxLength(50)]
        public string TrangThai { get; set; }

        [MaxLength(1000, ErrorMessage = "Phản hồi không được vượt quá 1000 ký tự")]
        public string? PhanHoiChuTro { get; set; }
    }

    public class CapNhatBaoCaoSuCoDto
    {
        public int? MaPhong { get; set; }

        [MaxLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự")]
        public string? TieuDe { get; set; }

        [MaxLength(1000, ErrorMessage = "Nội dung sự cố không được vượt quá 1000 ký tự")]
        public string? NoiDung { get; set; }

        [MaxLength(30)]
        public string? MucDo { get; set; }

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [MaxLength(1000, ErrorMessage = "Phản hồi không được vượt quá 1000 ký tự")]
        public string? PhanHoiChuTro { get; set; }
    }
}
