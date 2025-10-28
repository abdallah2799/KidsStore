using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IUserService _userService;

        public SettingsController(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Settings";
            ViewBag.CurrentUserName = User.Identity?.Name;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string newUserName)
        {
            if (string.IsNullOrWhiteSpace(newUserName))
                return Json(new { success = false, message = "Username cannot be empty" });

            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "Invalid user session" });

            // Get current user
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // Check if username is already taken
            if (await _userService.UsernameExistsAsync(newUserName, userId))
                return Json(new { success = false, message = "Username already exists" });

            // Update username
            var result = await _userService.UpdateUserAsync(userId, newUserName, user.Role, user.IsActive);

            if (!result)
                return Json(new { success = false, message = "Failed to update username" });

            return Json(new { success = true, message = "Username updated successfully. Please log in again." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "All fields are required" });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "New password and confirmation do not match" });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "Password must be at least 6 characters" });

            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "Invalid user session" });

            // Update password
            var result = await _userService.UpdatePasswordAsync(userId, currentPassword, newPassword);

            if (!result)
                return Json(new { success = false, message = "Current password is incorrect" });

            return Json(new { success = true, message = "Password updated successfully" });
        }
    }
}
