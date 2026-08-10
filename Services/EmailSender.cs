using System.Net;
using System.Net.Mail;

namespace eSchool.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["MailSettings:Host"];
            var username = _configuration["MailSettings:Username"];
            var password = _configuration["MailSettings:Password"];
            var fromEmail = _configuration["MailSettings:FromEmail"];
            var fromName = _configuration["MailSettings:FromName"] ?? "eSchool";
            var portText = _configuration["MailSettings:Port"];
            var enableSslText = _configuration["MailSettings:EnableSsl"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Chua cau hinh email gui trong appsettings.json");
            }

            if (!MailAddress.TryCreate(fromEmail, out var fromAddress))
                throw new InvalidOperationException("FromEmail trong appsettings.json khong dung dinh dang email");

            if (!MailAddress.TryCreate(toEmail, out var toAddress))
                throw new InvalidOperationException("Email trong tai khoan khong dung dinh dang email");

            var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
            var enableSsl = !bool.TryParse(enableSslText, out var parsedSsl) || parsedSsl;

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress.Address, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}
