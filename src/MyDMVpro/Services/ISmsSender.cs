using System.Threading.Tasks;

namespace MyDMVpro.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
