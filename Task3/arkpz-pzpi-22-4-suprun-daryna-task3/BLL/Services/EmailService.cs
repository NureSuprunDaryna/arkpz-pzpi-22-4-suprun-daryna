using BLL.Interfaces;
using Core.Configurations;
using Core.Models;
using Core.Templates;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task<Result> SendRegistrationConfirmationAsync(AppUser user, string confirmationURL)
        {
            return await SendAccountManagementConfirmationAsync(user, confirmationURL, "RegistrationConfirmation");
        }

        private async Task<Result> SendAccountManagementConfirmationAsync(AppUser user, string confirmationURL, string emailType)
        {
            if (user == null)
            {
                _logger.LogError("Failed to send an {emailType} email - user was null!", emailType);

                return new Result(false);
            }

            if (string.IsNullOrWhiteSpace(confirmationURL))
            {
                _logger.LogError("Failed to send an {emailType} email - confirmation link was null/empty/white spaces!", emailType);

                return new Result(false);
            }

            try
            {
                var userReferenceName = user.FirstName + " " + user.LastName;

                var subject = EmailTemplate.GetEmailSubject(emailType);
                var body = EmailTemplate.GetEmailBody(emailType, userReferenceName, confirmationURL);

                var emailResult = await FormEmailAsync(user.Email, subject, body);

                if (!emailResult.IsSuccessful)
                {
                    _logger.LogError("Failed to send email - letter wasn't formed");

                    return new Result(false);
                }

                return await SendEmailAsync(emailResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email! Error: {errorMessage}", ex.Message);

                return new Result(false);
            }
        }

        private async Task<Result<MimeMessage>> FormEmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("Forming email");

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Failed to form email - no destination was given");

                return new Result<MimeMessage>(false);
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogError("Failed to form email - no subject was given");

                return new Result<MimeMessage>(false);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Failed to form email - no body was given");

                return new Result<MimeMessage>(false);
            }

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html")
            {
                Text = body
            };

            _logger.LogInformation("Email formed successfully");

            return new Result<MimeMessage>(true, message);
        }

        private async Task<Result> SendEmailAsync(MimeMessage message)
        {
            _logger.LogInformation("Sending email: {subject}", message.Subject);

            var smtp = new SmtpClient();

            try
            {
                if (_emailSettings.UseStartTls)
                {
                    await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                }
                else if (_emailSettings.UseSSL)
                {
                    await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.SslOnConnect);
                }


                await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
                await smtp.SendAsync(message);

                _logger.LogInformation("Email sent successfully");

                await smtp.DisconnectAsync(true);

                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email! Error: {error}", ex.Message);

                return new Result(false);
            }
            finally
            {
                smtp.Dispose();
            }
        }

        public async Task<Result> SendNewAdminEmailAsync(string adminName, string tempPassword, string role, string toEmail)
        {
            if (string.IsNullOrWhiteSpace(adminName))
            {
                _logger.LogError("Failed to send new admin email - admin name is null or empty.");
                return new Result(false);
            }

            if (string.IsNullOrWhiteSpace(tempPassword))
            {
                _logger.LogError("Failed to send new admin email - temporary password is null or empty.");
                return new Result(false);
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                _logger.LogError("Failed to send new admin email - role is null or empty.");
                return new Result(false);
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Failed to send new admin email - destination email is null or empty.");
                return new Result(false);
            }

            try
            {
                // Generate subject and body for the email
                var subject = $"Welcome to the Admin Team - Role: {role}";
                var body = $@"
            <h3>Hello {adminName},</h3>
            <p>Congratulations on becoming an admin with the role of <b>{role}</b>!</p>
            <p>Below are your temporary credentials:</p>
            <ul>
                <li><b>Email:</b> {toEmail}</li>
                <li><b>Temporary Password:</b> {tempPassword}</li>
            </ul>
            <p>Please log in and update your password as soon as possible.</p>
            <p>Best regards,<br>The Team</p>";

                // Form the email
                var emailResult = await FormEmailAsync(toEmail, subject, body);

                if (!emailResult.IsSuccessful)
                {
                    _logger.LogError("Failed to send new admin email - email wasn't formed.");
                    return new Result(false);
                }

                // Send the email
                return await SendEmailAsync(emailResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send new admin email! Error: {error}", ex.Message);
                return new Result(false);
            }
        }

    }
}
