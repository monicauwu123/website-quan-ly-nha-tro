using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class TaoYeuCauThueDto
    {
        [Required]
        public int MaPhong { get; set; }

        [MaxLength(255)]
        public string? GhiChuNguoiDung { get; set; }
    }

    public class TuChoiYeuCauThueDto
    {
        [MaxLength(255)]
        public string? GhiChuChuTro { get; set; }
    }

    public class ChapNhanYeuCauThueDto
    {
        [Required]
        public DateTime NgayBatDau { get; set; }

        public DateTime? NgayKetThuc { get; set; }

        [Required]
        public decimal TienCoc { get; set; }

        [MaxLength(255)]
        public string? NoiDung { get; set; }

        [MaxLength(255)]
        public string? GhiChuChuTro { get; set; }
    }
}
