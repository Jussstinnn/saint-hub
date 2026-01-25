using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace SaintHub.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _opt;

        public SmtpEmailSender(IOptions<EmailOptions> opt)
        {
            _opt = opt.Value;
        }

        public async Task SendAsync(string[] to, string subject, string htmlBody)
        {
            if (to == null || to.Length == 0)
                return;

            // Config incompleta => no hacemos nada (evita caídas)
            if (string.IsNullOrWhiteSpace(_opt.Host) ||
                string.IsNullOrWhiteSpace(_opt.User) ||
                string.IsNullOrWhiteSpace(_opt.Password))
                return;

            var fromEmail = string.IsNullOrWhiteSpace(_opt.FromEmail) ? _opt.User : _opt.FromEmail;
            var fromName = string.IsNullOrWhiteSpace(_opt.FromName) ? "Saint Hub" : _opt.FromName;

            using var msg = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var t in to)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    msg.To.Add(t.Trim());
            }

            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = _opt.EnableSsl,
                Credentials = new NetworkCredential(_opt.User, _opt.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 8000
            };

            await client.SendMailAsync(msg);
        }
    }
}
