using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        [Required]
        public int MaHoaDon { get; set; }
        [ForeignKey("MaHoaDon")]
        public HoaDon HoaDon { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        [ForeignKey("MaNguoiThue")]
        public NguoiThue NguoiThue { get; set; }

        [Required]
        public DateTime NgayThanhToan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Required]
        [MaxLength(100)]
        public string HinhThucThanhToan { get; set; }

        [MaxLength(255)]
        public string? GhiChu { get; set; }

        // ── Biên lai thanh toán (upload bởi người dùng) ──────────────────────

        /// <summary>
        /// Đường dẫn ảnh biên lai (URL Cloudinary hoặc path local)
        /// </summary>
        [MaxLength(500)]
        public string? HinhAnhBienLai { get; set; }

        /// <summary>
        /// Mã giao dịch ngân hàng / ví điện tử
        /// </summary>
        [MaxLength(100)]
        public string? MaGiaoDich { get; set; }

        /// <summary>
        /// Trạng thái xác nhận:
        ///   ChoXacNhan | DaXacNhan | TuChoi | null (chưa gửi biên lai)
        /// </summary>
        [MaxLength(20)]
        public string? TrangThaiXacNhan { get; set; }

        /// <summary>
        /// Lý do từ chối (điền khi chủ trọ từ chối)
        /// </summary>
        [MaxLength(500)]
        public string? LyDoTuChoi { get; set; }

        /// <summary>
        /// MaNguoiDung của chủ trọ / admin đã xác nhận / từ chối
        /// </summary>
        public int? NguoiXacNhanId { get; set; }
        [ForeignKey("NguoiXacNhanId")]
        public User? NguoiXacNhan { get; set; }

        /// <summary>
        /// Thời điểm chủ trọ / admin xác nhận hoặc từ chối
        /// </summary>
        public DateTime? NgayXacNhan { get; set; }
    }
}
