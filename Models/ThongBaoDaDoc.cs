using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnSE104.Models
{
    /// <summary>
    /// Lưu trạng thái đã đọc theo từng người dùng nhận thông báo.
    /// Không dùng chung cột ThongBao.DaDoc nữa vì thông báo có thể gửi cho nhiều người/phòng/tất cả.
    /// </summary>
    public class ThongBaoDaDoc
    {
        [Key]
        public int ThongBaoDaDocId { get; set; }

        public int ThongBaoId { get; set; }

        [ForeignKey("ThongBaoId")]
        public ThongBao? ThongBao { get; set; }

        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public User? NguoiDung { get; set; }

        public DateTime NgayDoc { get; set; } = DateTime.Now;
    }
}
