using System.Threading.Tasks;

namespace SaintHub.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string[] to, string subject, string htmlBody);
    }
}
