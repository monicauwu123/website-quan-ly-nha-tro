using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    /// <summary>
    /// Thông báo hệ thống gửi đến người dùng / phòng / tất cả.
    /// LoaiThongBao: HoaDon | HopDong | DichVu | BaoCaoSuCo | ThuCong
    /// LoaiNguoiNhan: TatCa | Phong | NguoiDung
    /// TrangThai: HienThi | An
    /// </summary>
    public class ThongBao
    {
        [Key]
        public int ThongBaoId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string NoiDung { get; set; } = string.Empty;

        /// <summary>HoaDon | HopDong | DichVu | BaoCaoSuCo | ThuCong</summary>
        [MaxLength(50)]
        public string LoaiThongBao { get; set; } = "ThuCong";

        /// <summary>TatCa | Phong | NguoiDung</summary>
        [MaxLength(20)]
        public string LoaiNguoiNhan { get; set; } = "TatCa";

        /// <summary>Null = gửi theo LoaiNguoiNhan</summary>
        public int? NguoiNhanId { get; set; }

        [ForeignKey("NguoiNhanId")]
        public User? NguoiNhan { get; set; }

        public int? PhongId { get; set; }

        [ForeignKey("PhongId")]
        public Phong? Phong { get; set; }

        /// <summary>Người tạo thông báo (admin/chủ trọ). Null = hệ thống tự tạo.</summary>
        public int? NguoiTaoId { get; set; }

        [ForeignKey("NguoiTaoId")]
        public User? NguoiTao { get; set; }

        public bool DaDoc { get; set; } = false;

        public DateTime? NgayDoc { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>HienThi | An</summary>
        [MaxLength(20)]
        public string TrangThai { get; set; } = "HienThi";
    }
}
