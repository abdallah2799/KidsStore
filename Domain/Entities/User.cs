using System;
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

        // Utility methods
        public void SetPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            PasswordHash = Convert.ToBase64String(sha256.ComputeHash(bytes));
        }

        public bool VerifyPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = Convert.ToBase64String(sha256.ComputeHash(bytes));
            return hash == PasswordHash;
        }
    }
}
