using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Users";
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Json(new
            {
                success = true,
                data = users.Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    role = u.Role.ToString(),
                    isActive = u.IsActive,
                    createdAt = u.CreatedAt.ToString("yyyy-MM-dd")
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    role = (int)user.Role,
                    isActive = user.IsActive
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(string userName, string password, int role, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Username and password are required" });

            if (password.Length < 6)
                return Json(new { success = false, message = "Password must be at least 6 characters" });

            var userRole = (UserRole)role;
            var result = await _userService.CreateUserAsync(userName, password, userRole);

            if (!result)
                return Json(new { success = false, message = "Username already exists" });

            return Json(new { success = true, message = "User created successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string userName, int role, bool isActive, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Json(new { success = false, message = "Username is required" });

            var userRole = (UserRole)role;
            
            // Update user basic info
            var result = await _userService.UpdateUserAsync(id, userName, userRole, isActive);

            if (!result)
                return Json(new { success = false, message = "Failed to update user. Username may already exist." });

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                    return Json(new { success = false, message = "Password must be at least 6 characters" });

                var passwordResult = await _userService.ResetPasswordAsync(id, newPassword);
                if (!passwordResult)
                    return Json(new { success = false, message = "Failed to update password" });
            }

            return Json(new { success = true, message = "User updated successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Get current user ID from claims
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Prevent self-deletion
            if (id == currentUserId)
                return Json(new { success = false, message = "You cannot delete your own account" });

            var result = await _userService.DeleteUserAsync(id);

            if (!result)
                return Json(new { success = false, message = "Failed to delete user" });

            return Json(new { success = true, message = "User deactivated successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> CheckUsername(string userName, int? id = null)
        {
            var exists = await _userService.UsernameExistsAsync(userName, id);
            return Json(new { exists });
        }
    }
}
