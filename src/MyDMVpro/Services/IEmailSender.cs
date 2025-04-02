using System.Threading.Tasks;

namespace MyDMVpro.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
