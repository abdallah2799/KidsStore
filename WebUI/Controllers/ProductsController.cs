using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Products";
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Add Product";
            return View();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Edit Product";
            return View();
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            ViewBag.Title = "Product Details";
            return View();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            return RedirectToAction("Index");
        }
    }
}
