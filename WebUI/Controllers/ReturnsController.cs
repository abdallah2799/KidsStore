using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class ReturnsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
