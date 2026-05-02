using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class TaoYeuCauGiaHanDto
    {
        [Required]
        public int MaHopDong { get; set; }

        [Required]
        public DateTime NgayKetThucMoiDeXuat { get; set; }

        [MaxLength(500)]
        public string? GhiChuNguoiDung { get; set; }
    }

    public class DuyetYeuCauGiaHanDto
    {
        public DateTime? NgayKetThucMoi { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc mới phải lớn hơn hoặc bằng 0")]
        public decimal? TienCocMoi { get; set; }

        [MaxLength(1000)]
        public string? NoiDungDieuKhoanMoi { get; set; }

        [MaxLength(500)]
        public string? GhiChuChuTro { get; set; }
    }
}
