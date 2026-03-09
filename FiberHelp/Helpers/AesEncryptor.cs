using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FiberHelp.Helpers
{
    /// <summary>
    /// AES-256-CBC encryption for sensitive data at rest.
    /// The encryption key is read from the FIBERHELP_AES_KEY environment variable.
    /// If not set, a machine-specific key is derived automatically.
    /// </summary>
    public static class AesEncryptor
    {
        private const int KeySize = 32;  // AES-256
        private const int IvSize = 16;   // 128-bit IV

        private static byte[]? _cachedKey;

        /// <summary>
        /// Returns the AES key, reading from environment or deriving a machine-specific fallback.
        /// </summary>
        private static byte[] GetKey()
        {
            if (_cachedKey != null) return _cachedKey;

            var envKey = Environment.GetEnvironmentVariable("FIBERHELP_AES_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                // Derive a fixed-length key from the environment variable
                _cachedKey = DeriveKey(envKey);
            }
            else
            {
                // Fallback: derive from machine name + app identity (not ideal, but deterministic per machine)
                var seed = $"FiberHelp-{Environment.MachineName}-{Environment.UserName}";
                _cachedKey = DeriveKey(seed);
                System.Diagnostics.Debug.WriteLine("AesEncryptor: Using machine-derived key. Set FIBERHELP_AES_KEY for production.");
            }

            return _cachedKey;
        }

        private static byte[] DeriveKey(string passphrase)
        {
            // Use a fixed salt so the key is deterministic for the same passphrase
            var salt = Encoding.UTF8.GetBytes("FiberHelp.AES.Salt.v1");
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(passphrase),
                salt,
                50_000,
                HashAlgorithmName.SHA256,
                KeySize);
        }

        /// <summary>
        /// Encrypt a plaintext string. Returns a Base64 string containing IV + ciphertext.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            var key = GetKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext
            var result = new byte[IvSize + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
            Buffer.BlockCopy(cipherBytes, 0, result, IvSize, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypt a Base64 string that contains IV + ciphertext.
        /// </summary>
        public static string Decrypt(string cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64)) return string.Empty;

            var key = GetKey();
            var fullBytes = Convert.FromBase64String(cipherBase64);

            if (fullBytes.Length < IvSize + 1)
                throw new CryptographicException("Invalid encrypted data");

            var iv = new byte[IvSize];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, IvSize);

            var cipherBytes = new byte[fullBytes.Length - IvSize];
            Buffer.BlockCopy(fullBytes, IvSize, cipherBytes, 0, cipherBytes.Length);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Checks if data appears to be AES-encrypted (Base64 with sufficient length).
        /// </summary>
        public static bool IsEncrypted(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return false;
            if (data.StartsWith('{') || data.StartsWith('[')) return false; // plain JSON
            try
            {
                var bytes = Convert.FromBase64String(data);
                return bytes.Length > IvSize;
            }
            catch
            {
                return false;
            }
        }
    }
}
