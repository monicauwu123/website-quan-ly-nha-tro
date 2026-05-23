using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class ThongBaoCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string NoiDung { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LoaiThongBao { get; set; } = "ThuCong";

        /// <summary>TatCa | NhaTro | Phong | NguoiDung</summary>
        [Required]
        [MaxLength(20)]
        public string LoaiNguoiNhan { get; set; } = "TatCa";

        public int? NguoiNhanId { get; set; }
        public int? NhaTroId { get; set; }
        public int? PhongId { get; set; }
    }

    public class ThongBaoDto
    {
        public int ThongBaoId { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = string.Empty;
        public string LoaiNguoiNhan { get; set; } = string.Empty;
        public int? NguoiNhanId { get; set; }
        public string? TenNguoiNhan { get; set; }
        public int? PhongId { get; set; }
        public string? TenPhong { get; set; }
        public int? NguoiTaoId { get; set; }
        public string? TenNguoiTao { get; set; }
        public bool DaDoc { get; set; }
        public bool CoTheDanhDauDoc { get; set; }
        public DateTime? NgayDoc { get; set; }
        public DateTime NgayTao { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string LoaiThongBaoText { get; set; } = string.Empty;
        public string LoaiNguoiNhanText { get; set; } = string.Empty;
    }
}
