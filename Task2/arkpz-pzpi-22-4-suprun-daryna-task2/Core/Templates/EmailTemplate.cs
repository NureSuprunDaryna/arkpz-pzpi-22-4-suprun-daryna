namespace Core.Templates
{
    public static class EmailTemplate
    {
        // Статичні словники для шаблонів імейлу
        private static readonly Dictionary<string, string> _emailBody = new()
        {
            { "RegistrationConfirmation",
                @"<html>
                    <h2>Welcome, {0}!</h2>
                    <p>Thank you for registering. Click <a href='{1}'>this link</a> to confirm your account.</p>
                  </html>"
            }
        };

        private static readonly Dictionary<string, string> _emailSubject = new()
        {
            { "RegistrationConfirmation", "Confirm your registration" }
        };

        // Метод для отримання шаблону тіла імейлу
        public static string GetEmailBody(string emailType, params object[] args)
        {
            if (_emailBody.ContainsKey(emailType))
            {
                return string.Format(_emailBody[emailType], args);
            }

            throw new ArgumentException($"Email type '{emailType}' not found in templates.");
        }

        // Метод для отримання шаблону теми імейлу
        public static string GetEmailSubject(string emailType)
        {
            if (_emailSubject.ContainsKey(emailType))
            {
                return _emailSubject[emailType];
            }

            throw new ArgumentException($"Email type '{emailType}' not found in templates.");
        }
    }
}
