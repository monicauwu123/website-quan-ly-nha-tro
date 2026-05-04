namespace DoAnSE104.DTOs
{
    public class CreateHopDongDto
    {
        public int MaNguoiThue { get; set; }
        public int MaPhong { get; set; }
        //public int MaTrangThai { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public int? SoThangThue { get; set; }
        public decimal TienCoc { get; set; }
        public string? NoiDung { get; set; }
        
    }
}

