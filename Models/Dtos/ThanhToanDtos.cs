using Microsoft.AspNetCore.Http;

namespace DoAnSE104.Models.Dtos
{
    /// <summary>
    /// DTO người dùng gửi biên lai thanh toán (multipart/form-data)
    /// </summary>
    public class GuiBienLaiDto
    {
        public int MaHoaDon { get; set; }

        /// <summary>Số tiền đã chuyển</summary>
        public decimal TongTien { get; set; }

        public string KieuThanhToan { get; set; } = "ThanhToanHet";

        /// <summary>Hình thức: ChuyenKhoan | TienMat | Vi điện tử...</summary>
        public string HinhThucThanhToan { get; set; } = "ChuyenKhoan";

        /// <summary>Mã giao dịch ngân hàng (nếu có)</summary>
        public string? MaGiaoDich { get; set; }

        /// <summary>Ghi chú thêm</summary>
        public string? GhiChu { get; set; }

        /// <summary>Ảnh biên lai thanh toán</summary>
        public IFormFile? AnhBienLai { get; set; }
    }

    /// <summary>
    /// DTO chủ trọ / admin xác nhận hoặc từ chối biên lai
    /// </summary>
    public class XacNhanBienLaiDto
    {
        /// <summary>true = xác nhận, false = từ chối</summary>
        public bool ChapNhan { get; set; }

        /// <summary>Lý do từ chối (bắt buộc khi ChapNhan = false)</summary>
        public string? LyDoTuChoi { get; set; }
    }

    /// <summary>
    /// DTO trả về đầy đủ thông tin thanh toán (bao gồm biên lai)
    /// </summary>
    public class ThanhToanDto
    {
        public int MaThanhToan { get; set; }
        public int MaHoaDon { get; set; }
        public int MaNguoiThue { get; set; }
        public string TenNguoiThue { get; set; } = "";
        public DateTime NgayThanhToan { get; set; }
        public decimal TongTien { get; set; }
        public string HinhThucThanhToan { get; set; } = "";
        public string? GhiChu { get; set; }

        // Biên lai
        public string? HinhAnhBienLai { get; set; }
        public string? MaGiaoDich { get; set; }
        public string? TrangThaiXacNhan { get; set; }
        public string? TenTrangThaiXacNhan { get; set; }
        public string? LyDoTuChoi { get; set; }
        public int? NguoiXacNhanId { get; set; }
        public string? TenNguoiXacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
    }
}
