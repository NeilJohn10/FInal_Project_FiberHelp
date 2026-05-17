using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FiberHelp.Services
{
    public class EmailOtpService
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public EmailOtpService(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string otpCode, int expiryMinutes)
        {
            try
            {
                // Prefer values from configuration, fall back to environment variables if not present.
                var host = (_config["FIBERHELP_SMTP_HOST"] ?? Environment.GetEnvironmentVariable("FIBERHELP_SMTP_HOST") ?? string.Empty).Trim();
                var portText = (_config["FIBERHELP_SMTP_PORT"] ?? Environment.GetEnvironmentVariable("FIBERHELP_SMTP_PORT") ?? string.Empty).Trim();
                var user = (_config["FIBERHELP_SMTP_USER"] ?? Environment.GetEnvironmentVariable("FIBERHELP_SMTP_USER") ?? string.Empty).Trim();
                var pass = (_config["FIBERHELP_SMTP_PASS"] ?? Environment.GetEnvironmentVariable("FIBERHELP_SMTP_PASS") ?? string.Empty).Trim();
                var from = (_config["FIBERHELP_SMTP_FROM"] ?? Environment.GetEnvironmentVariable("FIBERHELP_SMTP_FROM") ?? string.Empty).Trim();

                // Gmail app passwords are commonly shown grouped by spaces. Normalize to raw token.
                pass = pass.Replace(" ", string.Empty);

                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(portText) ||
                    string.IsNullOrWhiteSpace(user) ||
                    string.IsNullOrWhiteSpace(pass) ||
                    string.IsNullOrWhiteSpace(from))
                {
                    return false;
                }

                if (!int.TryParse(portText, out var port) || port <= 0)
                    return false;

                var subject = "FiberHelp Password Reset Code";
                var body = $"Your FiberHelp password reset code is: {otpCode}\n\nThis code expires in {expiryMinutes} minutes.\n\nIf you did not request this reset, ignore this email.";

                using var message = new MailMessage(from, toEmail, subject, body);
                using var smtp = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(user, pass),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmailOtpService: Failed to send OTP email: {ex.Message}");
                return false;
            }
        }
    }
}
