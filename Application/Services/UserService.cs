using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var userRepo = _uow.Repository<User>();
            return await userRepo.AsQueryable()
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var userRepo = _uow.Repository<User>();
            return await userRepo.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var userRepo = _uow.Repository<User>();
            return await userRepo.AsQueryable()
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<bool> CreateUserAsync(string username, string password, UserRole role)
        {
            // Check if username already exists
            if (await UsernameExistsAsync(username))
                return false;

            var user = new User
            {
                UserName = username,
                Role = role,
                IsActive = true
            };

            user.SetPassword(password);

            var userRepo = _uow.Repository<User>();
            await userRepo.AddAsync(user);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateUserAsync(int id, string username, UserRole role, bool isActive)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(id);

            if (user == null)
                return false;

            // Check if username is taken by another user
            if (await UsernameExistsAsync(username, id))
                return false;

            user.UserName = username;
            user.Role = role;
            user.IsActive = isActive;

            userRepo.Update(user);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
                return false;

            // Verify current password
            if (!user.VerifyPassword(currentPassword))
                return false;

            // Set new password
            user.SetPassword(newPassword);

            userRepo.Update(user);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
                return false;

            // Set new password without requiring current password (admin function)
            user.SetPassword(newPassword);

            userRepo.Update(user);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(id);

            if (user == null)
                return false;

            // Instead of hard delete, we deactivate the user
            user.IsActive = false;
            userRepo.Update(user);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            var userRepo = _uow.Repository<User>();
            var query = userRepo.AsQueryable()
                .Where(u => u.UserName == username);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }
    }
}
