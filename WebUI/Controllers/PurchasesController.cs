using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PurchasesController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Purchases";
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Add Purchase";
            return View();
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            ViewBag.Title = "Purchase Details";
            return View();
        }
    }
}
