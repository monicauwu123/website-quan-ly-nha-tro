using System.ComponentModel.DataAnnotations;

namespace DoAnSE104.Models
{
    public class EmailLog
    {
        [Key]
        public int EmailLogId { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required]
        [MaxLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? RecipientName { get; set; }

        public DateTime? ReferenceDate { get; set; }

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Sent";

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
