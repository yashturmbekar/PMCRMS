using System.Security.Cryptography;
using System.Text;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Production-grade password hashing service using PBKDF2
    /// </summary>
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 100000; // OWASP recommendation
        private const char Delimiter = ';';

        /// <summary>
        /// Creates a hash from a password
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            return $"{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                var parts = passwordHash.Split(Delimiter);
                if (parts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var hash = Convert.FromBase64String(parts[1]);

                var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    KeySize
                );

                return CryptographicOperations.FixedTimeEquals(hash, hashToCompare);
            }
            catch
            {
                return false;
            }
        }
    }
}
