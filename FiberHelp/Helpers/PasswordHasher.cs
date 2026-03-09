using System;
using System.Security.Cryptography;
using System.Text;

namespace FiberHelp.Helpers
{
    /// <summary>
    /// Secure password hashing using PBKDF2 (RFC 2898) with automatic migration from legacy SHA256 hashes.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit salt
        private const int HashSize = 32; // 256-bit hash
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
        private const string Prefix = "PBKDF2:";

        /// <summary>
        /// Hash a password using PBKDF2 with a random salt.
        /// Output format: PBKDF2:{iterations}:{base64_salt}:{base64_hash}
        /// </summary>
        public static string Hash(string password)
        {
            var normalized = (password ?? string.Empty).Trim();
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(normalized),
                salt,
                Iterations,
                Algorithm,
                HashSize);

            return $"{Prefix}{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verify a password against a stored hash.
        /// Supports both PBKDF2 (new) and legacy SHA256 hex (old) formats.
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            var trimmedHash = storedHash.Trim();

            if (trimmedHash.StartsWith(Prefix, StringComparison.Ordinal))
                return VerifyPbkdf2(password, trimmedHash);

            // Legacy SHA256 fallback
            return VerifyLegacySha256(password, trimmedHash);
        }

        /// <summary>
        /// Returns true if the stored hash uses the legacy SHA256 format and should be upgraded.
        /// </summary>
        public static bool NeedsUpgrade(string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            return !storedHash.Trim().StartsWith(Prefix, StringComparison.Ordinal);
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            // Format: PBKDF2:{iterations}:{base64_salt}:{base64_hash}
            var parts = storedHash[Prefix.Length..].Split(':');
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out var iterations)) return false;

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var normalized = (password ?? string.Empty).Trim();
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(normalized),
                salt,
                iterations,
                Algorithm,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static bool VerifyLegacySha256(string password, string storedHex)
        {
            var normalized = (password ?? string.Empty).Trim();
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(normalized);
            var hash = sha.ComputeHash(bytes);
            var hex = Convert.ToHexString(hash);
            return string.Equals(hex, storedHex, StringComparison.OrdinalIgnoreCase);
        }
    }
}
