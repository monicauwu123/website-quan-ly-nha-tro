using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenDichVu { get; set; }

        [Range(0, 9999999999999, ErrorMessage = "Giá dịch vụ phải là số hợp lệ")]
        public float Tiendichvu { get; set; }

        /// <summary>
        /// Phân loại dùng chung: TinhPhi | TienIch | TienNghi.
        /// TinhPhi được dùng cho đăng ký dịch vụ và hóa đơn.
        /// TienIch/TienNghi dùng để hiển thị tiện ích nhà trọ, tiện nghi phòng.
        /// </summary>
        [MaxLength(30)]
        public string LoaiDichVu { get; set; } = "TinhPhi";

        /// <summary>Nhà trọ sở hữu dịch vụ này.</summary>
        public int? MaNhaTro { get; set; }

        [ForeignKey("MaNhaTro")]
        public virtual NhaTro? NhaTro { get; set; }

        /// <summary>Giữ lại để tương thích dữ liệu cũ. Dữ liệu mới ưu tiên MaNhaTro.</summary>
        public int? MaChuTro { get; set; }

        [ForeignKey("MaChuTro")]
        public virtual User? ChuTro { get; set; }

        /// <summary>
        /// Trạng thái dịch vụ: DangSuDung | NgungSuDung | DaXoa
        /// </summary>
        [MaxLength(30)]
        public string TrangThai { get; set; } = "DangSuDung";
    }
}
