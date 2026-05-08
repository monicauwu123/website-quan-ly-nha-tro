using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Models;
using DoAnSE104.Services;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public EmailController(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("cau-hinh")]
        public IActionResult LayCauHinhEmail()
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var username = emailSettings["Username"] ?? string.Empty;
            var senderEmail = emailSettings["SenderEmail"] ?? string.Empty;
            var password = emailSettings["Password"] ?? string.Empty;

            var daCauHinh = !string.IsNullOrWhiteSpace(username)
                && !string.IsNullOrWhiteSpace(senderEmail)
                && !string.IsNullOrWhiteSpace(password)
                && !LaPlaceholder(username)
                && !LaPlaceholder(senderEmail)
                && !LaPlaceholder(password);

            return Ok(ApiResponse<object>.Ok(new
            {
                smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com",
                smtpPort = emailSettings["SmtpPort"] ?? "587",
                useSsl = emailSettings["UseSsl"] ?? "false",
                username,
                senderEmail,
                senderName = emailSettings["SenderName"] ?? "Quản Lý Phòng Trọ",
                daCauHinh,
                emailReminderEnabled = _configuration.GetValue<bool?>("EmailReminder:Enabled") ?? false
            }));
        }

        [AllowAnonymous]
        [HttpPost("gui-thu")]
        public async Task<IActionResult> GuiThuEmail([FromBody] GuiThuEmailDto dto)
        {
            var toEmail = string.IsNullOrWhiteSpace(dto.Email)
                ? _configuration["EmailSettings:SenderEmail"]
                : dto.Email.Trim();

            if (string.IsNullOrWhiteSpace(toEmail))
                return BadRequest(ApiResponse<object>.Loi("Vui lòng nhập email nhận thử."));

            var subject = string.IsNullOrWhiteSpace(dto.TieuDe)
                ? "Gửi thử email - Quản Lý Phòng Trọ"
                : dto.TieuDe.Trim();

            var noiDung = string.IsNullOrWhiteSpace(dto.NoiDung)
                ? "Nếu bạn nhận được email này thì cấu hình Gmail SMTP đã hoạt động."
                : dto.NoiDung.Trim();

            var body = EmailService.WrapTemplate(subject, $@"
                <p>Xin chào,</p>
                <p>{EmailService.EscapeHtml(noiDung)}</p>
                <p style='color:#64748b;font-size:13px;'>Email này được gửi từ chức năng kiểm tra cấu hình SMTP của hệ thống quản lý nhà trọ.</p>");

            var sent = await _emailService.GuiEmailAsync(toEmail, toEmail, subject, body);

            _context.EmailLog.Add(new EmailLog
            {
                EventType = "GuiThu",
                EntityType = "Email",
                EntityId = 0,
                RecipientEmail = toEmail,
                RecipientName = toEmail,
                ReferenceDate = DateTime.Today,
                Subject = subject,
                Status = sent ? "Sent" : "Skipped",
                ErrorMessage = sent ? null : "SMTP chưa cấu hình App Password hoặc gửi thất bại. Kiểm tra console log để xem lỗi chi tiết.",
                SentAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            if (!sent)
                return BadRequest(ApiResponse<object>.Loi("Chưa gửi được email. Hãy kiểm tra EmailSettings:Password đã là Gmail App Password chưa, rồi chạy thử lại."));

            return Ok(ApiResponse<object>.Ok(new
            {
                email = toEmail,
                subject,
                sentAt = DateTime.Now
            }, "Đã gửi email thử thành công."));
        }

        [Authorize(Roles = "Admin,ChuTro")]
        [HttpGet("lich-su")]
        public async Task<IActionResult> LayLichSuEmail([FromQuery] int take = 50)
        {
            take = Math.Clamp(take, 1, 200);

            var logs = await _context.EmailLog
                .OrderByDescending(x => x.SentAt)
                .Take(take)
                .Select(x => new
                {
                    x.EmailLogId,
                    x.EventType,
                    x.EntityType,
                    x.EntityId,
                    x.RecipientEmail,
                    x.RecipientName,
                    x.ReferenceDate,
                    x.Subject,
                    x.Status,
                    x.ErrorMessage,
                    x.SentAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(logs));
        }

        private static bool LaPlaceholder(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var normalized = value.Trim().ToLowerInvariant();
            return normalized == "your-email@gmail.com"
                || normalized == "your-app-password"
                || normalized.Contains("nhap_app_password")
                || normalized.Contains("app-password")
                || normalized.Contains("app password")
                || normalized.Contains("placeholder");
        }
    }

    public class GuiThuEmailDto
    {
        public string? Email { get; set; }
        public string? TieuDe { get; set; }
        public string? NoiDung { get; set; }
    }
}
