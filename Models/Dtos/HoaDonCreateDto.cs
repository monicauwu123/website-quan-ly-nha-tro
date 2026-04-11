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
        [Required]
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


        [Required]
        public decimal TongTien {get;set;}

        [Required]
        public DateTime NgayLap { get; set; }

        [Required]
        [MaxLength(7)]
        public string KyHoaDon { get; set; }
    }

    
}

