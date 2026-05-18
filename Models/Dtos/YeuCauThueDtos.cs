using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class TaoYeuCauThueDto
    {
        [Required]
        public int MaPhong { get; set; }

        [MaxLength(255)]
        public string? GhiChuNguoiDung { get; set; }

        [Range(1, 60, ErrorMessage = "Số tháng muốn thuê phải từ 1 đến 60")]
        public int SoThangMuonThue { get; set; } = 1;

        public DateTime? NgayBatDauMongMuon { get; set; }
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

        [Range(1, 60, ErrorMessage = "Số tháng thuê phải từ 1 đến 60")]
        public int? SoThangThue { get; set; }

        [Required]
        public decimal TienCoc { get; set; }

        [MaxLength(1000)]
        public string? NoiDung { get; set; }

        [MaxLength(255)]
        public string? GhiChuChuTro { get; set; }
    }

    public class TuChoiHopDongYeuCauThueDto
    {
        [MaxLength(255)]
        public string? GhiChuNguoiDung { get; set; }
    }
}
