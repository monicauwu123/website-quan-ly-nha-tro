using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class HoaDon
    {
        [Key]
        public int MaHoaDon { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        public NguoiThue NguoiThue { get; set; }

        [Required]
        public int MaPhong { get; set; }
        public Phong Phong { get; set; }

        public int? MaDien { get; set; }
        public ChiSoDien? ChiSoDien { get; set; }

        public int? MaNuoc { get; set; }
        public ChiSoNuoc? ChiSoNuoc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienPhatSinhKhac { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        public string LoaiHoaDon { get; set; } = "HangThang";

        // Tổng tiền phải trả cho hóa đơn.
        // Hóa đơn thuê phòng: tiền phòng + phát sinh khác.
        // Hóa đơn hằng tháng: tiền điện + tiền nước + dịch vụ đã chọn + phát sinh khác.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        // Ngày lập hóa đơn
        [Required]
        public DateTime NgayLap { get; set; }

        // Kỳ hóa đơn, ví dụ: "2023-05"
        [Required]
        [MaxLength(7)]
        public string KyHoaDon { get; set; }

        // Bộ sưu tập chi tiết các khoản tiền trong hóa đơn (tiền phòng, tiền điện, tiền nước, dịch vụ...)
        public ICollection<ChiTietHoaDon> ChiTietHoaDon { get; set; }

        /// <summary>
        /// Trạng thái hóa đơn: ChuaThanhToan | DaThanhToan | Huy
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TrangThai { get; set; } = "ChuaThanhToan";
    }
}

