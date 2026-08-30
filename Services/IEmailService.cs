using System.Threading.Tasks;

namespace TechnoVIS.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
}
