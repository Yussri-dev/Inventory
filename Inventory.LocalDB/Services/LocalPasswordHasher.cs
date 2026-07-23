using System.Security.Cryptography;

namespace Inventory.LocalDB.Services
{
    public static class LocalPasswordHasher
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int DefaultIterations = 100_000;

        public static (string Hash, string Salt, int Iterations) HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));

            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                DefaultIterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return (
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes),
                DefaultIterations);
        }

        public static bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt,
            int iterations)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            if (string.IsNullOrWhiteSpace(storedSalt))
                return false;

            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedHashBytes = Convert.FromBase64String(storedHash);

            var actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHashBytes.Length);

            return CryptographicOperations.FixedTimeEquals(
                actualHashBytes,
                expectedHashBytes);
        }
    }
}