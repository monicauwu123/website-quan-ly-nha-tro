using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class NguoiThue
    {
        [Key]
        public int MaNguoiThue { get; set; }

        [Required]
        [MaxLength(100)]
        public string HoTen { get; set; }

        [MaxLength(20)]
        public string? CCCD { get; set; }

        [MaxLength(15)]
        public string? SDT { get; set; }

        public DateTime? NgaySinh { get; set; }

        [MaxLength(255)]
        public string? DiaChi { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(10)]
        public string? GioiTinh { get; set; }

        [MaxLength(50)]
        public string? QuocTich { get; set; }

        [MaxLength(100)]
        public string? NoiCongTac { get; set; }

        [MaxLength(500)]
        public string? AnhCccdMatTruoc { get; set; }

        [MaxLength(500)]
        public string? AnhCccdMatSau { get; set; }

        public int MaPhong { get; set; }

        /// <summary>FK tới User (NguoiDung account liên kết)</summary>
        public int? MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual User? NguoiDungTK { get; set; }

        /// <summary>
        /// Trạng thái khách thuê: DangThue | KhongHoatDong | DaXoa
        /// </summary>
        [MaxLength(30)]
        public string TrangThai { get; set; } = "DangThue";
    }
}
