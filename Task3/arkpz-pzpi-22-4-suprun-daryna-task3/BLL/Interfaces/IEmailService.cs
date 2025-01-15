using Core.Models;

namespace BLL.Interfaces
{
    public interface IEmailService
    {
        Task<Result> SendRegistrationConfirmationAsync(AppUser user, string confirmationURL);
        Task<Result> SendNewAdminEmailAsync(string adminName, string tempPassword, string role, string toEmail);
    }
}
