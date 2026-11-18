using System.Security.Cryptography;

namespace POSSystem.WPF.Business.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;
        private const char Separator = '$';
        private const string CurrentPrefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return string.Join(
                Separator,
                CurrentPrefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            if (IsCurrentHash(storedHash))
                return VerifyPbkdf2(password, storedHash);

            try
            {
                var legacyHash = HashLegacySha256(password);
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(legacyHash),
                    Convert.FromBase64String(storedHash));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool NeedsRehash(string? storedHash)
        {
            return !IsCurrentHash(storedHash);
        }

        public static bool IsPasswordHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (IsCurrentHash(value))
                return true;

            try
            {
                return Convert.FromBase64String(value).Length == HashSize;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string HashLegacySha256(string password)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            var parts = storedHash.Split(Separator);
            if (parts.Length != 4 || parts[0] != CurrentPrefix)
                return false;

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsCurrentHash(string? storedHash)
        {
            return storedHash?.StartsWith($"{CurrentPrefix}{Separator}", StringComparison.Ordinal) == true;
        }
    }
}
