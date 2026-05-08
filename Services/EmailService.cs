using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DoAnSE104.Services
{
    public interface IEmailService
    {
        Task GuiEmailResetMatKhau(string toEmail, string hoTen, string token, string resetUrl);
        Task<bool> GuiEmailAsync(string toEmail, string hoTen, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task GuiEmailResetMatKhau(string toEmail, string hoTen, string token, string resetUrl)
        {
            var htmlBody = WrapTemplate("Đặt lại mật khẩu", $@"
                <p>Xin chào <strong>{EscapeHtml(hoTen)}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Sử dụng mã OTP dưới đây, có hiệu lực trong <strong>15 phút</strong>:</p>
                <div style='background:#f0f4ff;border:1px dashed #3b82f6;border-radius:6px;padding:16px;text-align:center;margin:20px 0;'>
                    <span style='font-size:28px;font-weight:bold;letter-spacing:6px;color:#1d4ed8;'>{EscapeHtml(token)}</span>
                </div>
                <p>Hoặc bấm vào nút bên dưới để đặt lại mật khẩu trực tiếp:</p>
                <p style='text-align:center;'>
                    <a href='{EscapeHtml(resetUrl)}' style='display:inline-block;padding:12px 28px;background:#3b82f6;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;margin:10px 0;'>Đặt lại mật khẩu</a>
                </p>
                <p style='color:#888;font-size:13px;'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>");

            var sent = await GuiEmailAsync(toEmail, hoTen, "Đặt lại mật khẩu - Quản Lý Phòng Trọ", htmlBody);
            if (!sent)
                throw new Exception("Không thể gửi email. Vui lòng kiểm tra cấu hình SMTP.");
        }

        public async Task<bool> GuiEmailAsync(string toEmail, string hoTen, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return false;

            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];

            if (string.IsNullOrWhiteSpace(senderEmail)
                || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password)
                || IsPlaceholder(username)
                || IsPlaceholder(password)
                || IsPlaceholder(senderEmail))
            {
                _logger.LogWarning("Bỏ qua gửi email tới {Email} vì EmailSettings chưa được cấu hình SMTP thật. Hãy nhập Gmail App Password trong EmailSettings:Password.", toEmail);
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"] ?? "Quản Lý Phòng Trọ", senderEmail));
            message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(hoTen) ? toEmail : hoTen, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                var host = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var port = int.TryParse(emailSettings["SmtpPort"], out var parsedPort) ? parsedPort : 587;
                var useSsl = bool.TryParse(emailSettings["UseSsl"], out var parsedUseSsl) && parsedUseSsl;
                var secureOption = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await client.ConnectAsync(host, port, secureOption);
                await client.AuthenticateAsync(username.Trim(), password.Replace(" ", string.Empty).Trim());
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Đã gửi email tới {Email}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email tới {Email}: {Subject}", toEmail, subject);
                return false;
            }
        }

        private static bool IsPlaceholder(string? value)
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

        public static string WrapTemplate(string title, string bodyHtml)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
</head>
<body style='font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;'>
  <div style='max-width:600px;margin:32px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);'>
    <div style='background:#0d9488;padding:22px;text-align:center;color:#fff;'>
      <h1 style='margin:0;font-size:22px;'>{EscapeHtml(title)}</h1>
    </div>
    <div style='padding:28px;color:#333;line-height:1.6;'>
      {bodyHtml}
    </div>
    <div style='padding:16px;text-align:center;color:#999;font-size:12px;border-top:1px solid #eee;'>
      &copy; {DateTime.Now.Year} Quản Lý Phòng Trọ - Email này được gửi tự động, vui lòng không trả lời.
    </div>
  </div>
</body>
</html>";
        }

        public static string EscapeHtml(string? value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;")
                    .Replace("\"", "&quot;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");
        }
    }
}
