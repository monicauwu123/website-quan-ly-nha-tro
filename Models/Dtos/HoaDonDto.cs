namespace DoAnSE104.Models.Dtos
{
    public class HoaDonDto
    {
    
        public int MaHoaDon { get; set; }
        public string TenPhong { get; set; }
        public string TenNguoiThue { get; set; }
        public decimal TienDichVu { get; set; } = 0;
        public decimal TienNuoc { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienPhatSinhKhac { get; set; } = 0;
        public DateTime NgayLap { get; set; }
        public string KyHoaDon { get; set; }
        public decimal TongTien { get; set; }
        
    }

}

