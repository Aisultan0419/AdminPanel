using MailKit.Net.Smtp;
using MimeKit;

namespace UserManagement.Infrastructure
{
    public class EmailService
    {
        private readonly string _senderEmail;
        private readonly string _appPassword;
        private const string _smtpServer = "smtp.gmail.com";
        private const int _smtpPort = 587;

        public EmailService()
        {
            _senderEmail = Environment.GetEnvironmentVariable("EMAIL_ADDRESS") 
                ?? throw new InvalidOperationException("EMAIL_ADDRESS environment variable is not set.");
            _appPassword = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD") 
                ?? throw new InvalidOperationException("EMAIL_APP_PASSWORD environment variable is not set.");
        }

        public async Task SendVerificationEmailAsync(string userEmail, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("User Management", _senderEmail));
            message.To.Add(new MailboxAddress(string.Empty, userEmail));
            message.Subject = "Verification Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"<p>This is your verification code: <strong>{code}</strong></p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_senderEmail, _appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
