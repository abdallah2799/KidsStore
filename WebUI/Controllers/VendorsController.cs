using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models.ViewModels;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VendorsController : Controller
    {
        private readonly IVendorService _vendorService;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(IVendorService vendorService, ILogger<VendorsController> logger)
        {
            _vendorService = vendorService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8)
        {
            var vendors = await _vendorService.GetAllAsync();

            var totalVendors = vendors.Count;
            var totalPages = (int)Math.Ceiling(totalVendors / (double)pageSize);

            var pagedVendors = vendors
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VendorViewModel
                {
                    Id = v.Id,
                    Name = v.Name,
                    CodePrefix = v.CodePrefix,
                    ContactInfo = v.ContactInfo,
                    Address = v.Address,
                    Notes = v.Notes
                })
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedVendors);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] VendorViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            try
            {
                var vendor = new Vendor
                {
                    Name = model.Name,
                    CodePrefix = model.CodePrefix,
                    ContactInfo = model.ContactInfo,
                    Address = model.Address,
                    Notes = model.Notes
                };

                var added = await _vendorService.AddAsync(vendor);
                return Json(added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add vendor failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] VendorViewModel model)
        {
            var vendor = new Vendor
            {
                Id = model.Id,
                Name = model.Name,
                CodePrefix = model.CodePrefix,
                ContactInfo = model.ContactInfo,
                Address = model.Address,
                Notes = model.Notes
            };

            var updated = await _vendorService.UpdateAsync(vendor);
            if (updated == null)
                return NotFound();

            return Json(updated);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _vendorService.DeleteAsync(id);
            return success ? Ok() : NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> CheckName(string name)
        {
            var exists = await _vendorService.IsNameExistsAsync(name);
            return Json(new { exists });
        }
    }
}
