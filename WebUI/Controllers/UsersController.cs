using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Users";
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Add User";
            return View();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Edit User";
            return View();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            return RedirectToAction("Index");
        }
    }
}
