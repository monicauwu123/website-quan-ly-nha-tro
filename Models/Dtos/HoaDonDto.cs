using System;
using System.Collections.Generic;

namespace DoAnSE104.Models.Dtos
{
    public class HoaDonDto
    {
        public int MaHoaDon { get; set; }
        public int MaNguoiThue { get; set; }
        public int MaPhong { get; set; }
        public string TenPhong { get; set; }
        public string TenNguoiThue { get; set; }
        public string LoaiHoaDon { get; set; } = "HangThang";
        public string TenLoaiHoaDon { get; set; } = "Hóa đơn hằng tháng";
        public decimal TienDichVu { get; set; } = 0;
        public decimal TienNuoc { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienPhatSinhKhac { get; set; } = 0;
        public DateTime NgayLap { get; set; }
        public string KyHoaDon { get; set; }
        public decimal TongTien { get; set; }
        public List<string> DichVuSuDung { get; set; } = new();
        public decimal DaThanhToan { get; set; } = 0;
        public decimal ConLai { get; set; } = 0;
        public string TrangThaiThanhToan { get; set; } = "Chưa thanh toán";

        /// <summary>
        /// Trạng thái hóa đơn: ChuaThanhToan | DaThanhToan | Huy
        /// </summary>
        public string TrangThai { get; set; } = "ChuaThanhToan";

        public int? MaChuTro { get; set; }
        public string? TenChuTro { get; set; }
        public string? TenNganHang { get; set; }
        public string? MaNganHang { get; set; }
        public string? SoTaiKhoan { get; set; }
        public string? TenChuTaiKhoan { get; set; }
        public string? NoiDungChuyenKhoan { get; set; }
        public string? QrThanhToanUrl { get; set; }

        /// <summary>
        /// True nếu đang có biên lai ChoXacNhan — dùng để ẩn nút "Gửi biên lai" trên FE.
        /// </summary>
        public bool DaCoBienLaiChoXacNhan { get; set; } = false;
    }
}
