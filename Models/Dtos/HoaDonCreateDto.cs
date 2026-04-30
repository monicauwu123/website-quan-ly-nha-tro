using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models.Dtos
{
    public class HoaDonCreateDto
    {
        [Required]
        public int MaNguoiThue { get; set; }

        [Required]
        public int MaPhong { get; set; }

        // "ThuePhong" = hóa đơn thuê phòng, chỉ tính tiền phòng + phát sinh khác.
        // "HangThang" = hóa đơn hằng tháng, chỉ tính điện/nước/dịch vụ/phát sinh khác.
        [Required]
        [MaxLength(20)]
        public string LoaiHoaDon { get; set; } = "HangThang";

        [Range(0, double.MaxValue)]
        public decimal TienPhong { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TienDien { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TienNuoc { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TienPhatSinhKhac { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal TienDichVu { get; set; } = 0;

        // Danh sách dịch vụ thực tế đã sử dụng để tính hóa đơn.
        // Frontend gửi các MaDichVu được chọn trong form lập hóa đơn.
        public List<int> MaDichVuSuDung { get; set; } = new();

        [Required]
        public decimal TongTien { get; set; }

        [Required]
        public DateTime NgayLap { get; set; }

        [Required]
        [MaxLength(7)]
        public string KyHoaDon { get; set; }
    }
}
