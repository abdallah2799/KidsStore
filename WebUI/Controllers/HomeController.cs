using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            // 🩹 FIX: restore from cookies if session expired
            if (string.IsNullOrEmpty(userName))
            {
                var cookieUserId = HttpContext.Request.Cookies["UserId"];
                var cookieUserName = HttpContext.Request.Cookies["UserName"];
                var cookieUserRole = HttpContext.Request.Cookies["UserRole"];

                if (!string.IsNullOrEmpty(cookieUserId) && !string.IsNullOrEmpty(cookieUserName))
                {
                    HttpContext.Session.SetInt32("UserId", int.Parse(cookieUserId));
                    HttpContext.Session.SetString("UserName", cookieUserName);
                    HttpContext.Session.SetString("UserRole", cookieUserRole ?? "Cashier"); // fallback role

                    userName = cookieUserName;
                    role = cookieUserRole;
                }
                else
                {
                    // no session, no cookies → go login
                    return RedirectToAction("Login", "Account");
                }
            }

            ViewBag.CurrentUser = userName;
            ViewBag.CurrentRole = role;
            ViewBag.Title = "Dashboard";
            ViewBag.UserInitials = string.Concat(userName.Take(2)).ToUpper();

            return View();
        }


    }
}
