using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Backup & Restore";
            return View();
        }
    }
}
