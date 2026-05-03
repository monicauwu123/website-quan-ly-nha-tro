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

        // NgÃ y láº­p hÃ³a Ä‘Æ¡n
        [Required]
        public DateTime NgayLap { get; set; }

        // Ká»³ hÃ³a Ä‘Æ¡n, vÃ­ dá»¥: "2023-05"
        [Required]
        [MaxLength(7)]
        public string KyHoaDon { get; set; }

        // Bá»™ sÆ°u táº­p chi tiáº¿t cÃ¡c khoáº£n tiá»n trong hÃ³a Ä‘Æ¡n (tiá»n phÃ²ng, tiá»n Ä‘iá»‡n, tiá»n nÆ°á»›c, dá»‹ch vá»¥...)
        public ICollection<ChiTietHoaDon> ChiTietHoaDon { get; set; }

        /// <summary>
        /// Trạng thái hóa đơn: ChuaThanhToan | DaThanhToan | Huy
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TrangThai { get; set; } = "ChuaThanhToan";
    }
}

