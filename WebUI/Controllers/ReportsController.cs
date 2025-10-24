using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        public IActionResult Sales(DateTime? from, DateTime? to)
        {
            ViewBag.Title = "Sales Reports";
            return View("Sales");
        }

        public IActionResult Inventory()
        {
            ViewBag.Title = "Inventory Reports";
            return View("Inventory");
        }
    }
}
