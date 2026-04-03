using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models
{
    public class TrangThai
    {
        [Key]
        public int MaTrangThai { get; set; }
        [Required]
        [MaxLength(50)]
        public string TenTrangThai { get; set; }
     
    }
} 
