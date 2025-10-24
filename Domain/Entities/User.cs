using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Entities
{
    public enum UserRole
    {
        Admin = 1,
        Cashier = 2
    }

    public class User : BaseEntity
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();

        // Constants for hashing
        [NotMapped] private const int SaltSize = 16;
        [NotMapped] private const int KeySize = 32;
        [NotMapped] private const int Iterations = 100_000;
        [NotMapped] private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

        // Utility methods
        
        public void SetPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
            PasswordHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
        public bool VerifyPassword(string password)
        {
            var parts = PasswordHash.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);

            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}