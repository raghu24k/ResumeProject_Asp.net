using System.Threading.Tasks;

namespace ResumeProject.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
