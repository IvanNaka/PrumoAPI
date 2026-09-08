using System;
using System.Security.Cryptography;

namespace Prumo.Application.Common
{
    /// <summary>
    /// Simple PBKDF2-based password hasher. Avoids introducing extra NuGet dependencies
    /// while still using a salted, iterated hash suitable for credential storage.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join(
                '.',
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            var parts = hashedPassword.Split('.', 3);
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out var iterations))
                return false;

            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);

            var computedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);

            return CryptographicOperations.FixedTimeEquals(computedKey, key);
        }
    }
}
