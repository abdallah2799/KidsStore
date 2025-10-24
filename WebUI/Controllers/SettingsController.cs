using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Settings";
            return View();
        }
    }
}
