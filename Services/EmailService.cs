using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DoAnSE104.Services
{
    public interface IEmailService
    {
        Task GuiEmailResetMatKhau(string toEmail, string hoTen, string token, string resetUrl);
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
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"] ?? "Quản Lý Phòng Trọ",
                emailSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress(hoTen, toEmail));
            message.Subject = "Đặt lại mật khẩu - Quản Lý Phòng Trọ";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 0; }}
    .container {{ max-width: 600px; margin: 40px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,.1); }}
    .header {{ background: #3b82f6; padding: 24px; text-align: center; color: #fff; }}
    .header h1 {{ margin: 0; font-size: 22px; }}
    .body {{ padding: 32px; color: #333; }}
    .body p {{ line-height: 1.6; }}
    .token-box {{ background: #f0f4ff; border: 1px dashed #3b82f6; border-radius: 6px; padding: 16px; text-align: center; margin: 20px 0; }}
    .token-box span {{ font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #1d4ed8; }}
    .btn {{ display: inline-block; padding: 12px 28px; background: #3b82f6; color: #fff; border-radius: 6px; text-decoration: none; font-weight: bold; margin: 10px 0; }}
    .footer {{ padding: 16px; text-align: center; color: #999; font-size: 12px; border-top: 1px solid #eee; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Đặt lại mật khẩu</h1>
    </div>
    <div class='body'>
      <p>Xin chào <strong>{hoTen}</strong>,</p>
      <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Sử dụng mã OTP dưới đây (có hiệu lực trong <strong>15 phút</strong>):</p>
      <div class='token-box'>
        <span>{token}</span>
      </div>
      <p>Hoặc bấm vào nút bên dưới để đặt lại mật khẩu trực tiếp:</p>
      <p style='text-align:center;'>
        <a href='{resetUrl}' class='btn'>Đặt lại mật khẩu</a>
      </p>
      <p style='color:#888; font-size:13px;'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>
    </div>
    <div class='footer'>
      &copy; {DateTime.Now.Year} Quản Lý Phòng Trọ — Email này được gửi tự động, vui lòng không trả lời.
    </div>
  </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                var host = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var port = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var useSsl = bool.Parse(emailSettings["UseSsl"] ?? "false");
                var username = emailSettings["Username"];
                var password = emailSettings["Password"];

                var secureOption = useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(host, port, secureOption);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Đã gửi email reset mật khẩu tới {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email tới {Email}", toEmail);
                throw new Exception("Không thể gửi email. Vui lòng thử lại sau.");
            }
        }
    }
}
