using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WebUI.Models;

namespace WebUI.Controllers
{
    // 🔒 Require login for this controller
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize(Roles = "Admin")] // 🔐 only Admins can access the Dashboard
        public IActionResult Index()
        {
            // Read data directly from claims (created at login)
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";

            // If somehow not authenticated, fallback to login
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction("Login", "Account");

            // Pass to the layout via ViewBag
            ViewBag.CurrentUser = userName;
            ViewBag.CurrentRole = role;
            ViewBag.Title = "Dashboard";
            ViewBag.UserInitials = string.Concat(userName.Take(2)).ToUpper();

            return View();
        }

        // Optional: shared route for testing authorization
        [Authorize(Roles = "Cashier,Admin")]
        public IActionResult TestAuth()
        {
            return Content($"✅ Authorized as {User.Identity?.Name} ({User.FindFirstValue(ClaimTypes.Role)})");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
