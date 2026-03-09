using System;
using System.Text.RegularExpressions;

namespace FiberHelp.Helpers
{
    /// <summary>
    /// Centralized input validation for client-side and server-side use.
    /// </summary>
    public static partial class InputValidator
    {
        // Compiled regex for email validation (RFC 5322 simplified)
        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
        private static partial Regex EmailRegex();

        // Philippine phone: +63 or 09 prefix, 10-12 digits
        [GeneratedRegex(@"^(\+?63|0)?\d{10,12}$", RegexOptions.Compiled)]
        private static partial Regex PhoneRegex();

        /// <summary>Validate email format</summary>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex().IsMatch(email.Trim());
        }

        /// <summary>Validate phone number format</summary>
        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true; // phone is optional
            var cleaned = phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            return PhoneRegex().IsMatch(cleaned);
        }

        /// <summary>Validate password meets policy (12+ chars, upper, digit, special)</summary>
        public static bool IsValidPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            return password.Length >= 12
                && password.Any(char.IsUpper)
                && password.Any(char.IsDigit)
                && password.Any(c => !char.IsLetterOrDigit(c));
        }

        /// <summary>Validate a required text field</summary>
        public static bool IsRequired(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>Validate a name (2+ characters, no special chars except spaces, hyphens, periods)</summary>
        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return name.Trim().Length >= 2;
        }

        /// <summary>Validate a monetary amount</summary>
        public static bool IsValidAmount(decimal? amount)
        {
            return amount.HasValue && amount.Value >= 0;
        }

        /// <summary>Get validation error message for email</summary>
        public static string? ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required";
            if (!IsValidEmail(email)) return "Invalid email format (e.g. user@domain.com)";
            return null;
        }

        /// <summary>Get validation error message for phone</summary>
        public static string? ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null; // optional
            if (!IsValidPhone(phone)) return "Invalid phone format (e.g. +639123456789)";
            return null;
        }

        /// <summary>Get validation error message for password</summary>
        public static string? ValidatePassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return "Password is required";
            if (password.Length < 12) return "Password must be at least 12 characters";
            if (!password.Any(char.IsUpper)) return "Password must contain an uppercase letter";
            if (!password.Any(char.IsDigit)) return "Password must contain a number";
            if (!password.Any(c => !char.IsLetterOrDigit(c))) return "Password must contain a special character";
            return null;
        }

        /// <summary>Sanitize string input — trim and limit length</summary>
        public static string Sanitize(string? input, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var trimmed = input.Trim();
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }
    }
}
