using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models
{
    public class User
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(100)]
        public string MatKhau { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(50)]
        public string HoTen { get; set; }

        [StringLength(15)]
        public string SoDienThoai { get; set; }

        /// <summary>Admin | ChuTro | NguoiDung</summary>
        public string VaiTro { get; set; } = "NguoiDung";

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual ICollection<NhaTro> DanhSachNhaTro { get; set; }
        public virtual ICollection<NguoiThue> DanhSachNguoiThue { get; set; }
    }
}
