using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
                return RedirectToAction("Index", "Home");

            // Fallback: check cookies
            var cookieUserId = HttpContext.Request.Cookies["UserId"];
            var cookieRole = HttpContext.Request.Cookies["UserRole"];

            if (!string.IsNullOrEmpty(cookieUserId) && !string.IsNullOrEmpty(cookieRole))
            {
                HttpContext.Session.SetInt32("UserId", int.Parse(cookieUserId));
                HttpContext.Session.SetString("UserRole", cookieRole);
                return RedirectToAction("Index", cookieRole == "Admin" ? "Home" : "Sales");
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            Console.WriteLine(model.RememberMe);
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.LoginAsync(model.UserName,model.Password);

            if (result!=null)
            {
                if (model.RememberMe)
                {
                    Response.Cookies.Append("UserId", result.Id.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true
                    });

                    Response.Cookies.Append("UserName", result.UserName, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true
                    });

                    Response.Cookies.Append("UserRole", result.Role.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true
                    });
                }

                return RedirectToAction("Index", "Home");  // ← this must happen in both cases
            }

            
            return View(model);
        }



        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _accountService.CreateUserAsync(model.UserName, model.Password, model.Role);
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
