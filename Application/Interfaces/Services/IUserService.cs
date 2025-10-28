using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(string username, string password, UserRole role);
        Task<bool> UpdateUserAsync(int id, string username, UserRole role, bool isActive);
        Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    }
}
