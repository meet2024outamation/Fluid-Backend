using SharedKernel.Models;

namespace SharedKernel.Services
{
    public interface IEmailServices
    {
        Task SendEmailAsync(EmailVM email);
        Task SendEmailWithTemplateAsync(EmailTemplate emailTemplate, object obj);
    }
}
