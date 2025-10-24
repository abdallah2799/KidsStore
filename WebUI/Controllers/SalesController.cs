using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin,Cashier")]
    public class SalesController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Point of Sale";
            return View();
        }
    }
}
