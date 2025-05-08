using System.Net.Mail;
using System.Net;

namespace WebApplication_SecurityApi.Service.Email
{
    public class EmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using var client = new SmtpClient(_config["EmailSettings:SmtpServer"])
            {
                Port = int.Parse(_config["EmailSettings:SmtpPort"] ?? ""),
                Credentials = new NetworkCredential(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderPassword"]),
                EnableSsl = bool.Parse(_config["EmailSettings:UseSSL"]??""),
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["EmailSettings:SenderEmail"] ?? ""),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }
}
