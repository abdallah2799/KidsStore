using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models.ViewModels;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Products";
            return View();
        }

        // GET: /Products/IsCodeAvailable
        [HttpGet]
        public async Task<IActionResult> IsCodeAvailable(string code, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "Code is required" });

            var exists = await _productService.CodeExistsAsync(code, excludeId);
            return Json(new { available = !exists });
        }

        // GET: /Products/GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllWithDetailsAsync();

            // Enrich with last sold dates
            var ids = products.Select(p => p.Id).ToList();
            var lastSoldMap = await _productService.GetLastSoldDatesAsync(ids);

            var result = products.Select(p => {
                // Filter out variants with 0 stock for display only
                // Note: Zero-stock variants remain in database for historical data
                // and will still be shown when editing the product
                var inStockVariants = p.Variants?.Where(v => v.Stock > 0).ToList() ?? new List<ProductVariant>();
                
                return new ProductViewModel
                {
                    Id = p.Id,
                    Code = p.Code,
                    Description = p.Description,
                    VendorId = p.VendorId,
                    VendorName = p.Vendor?.Name,
                    BuyingPrice = p.BuyingPrice,
                    SellingPrice = p.SellingPrice,
                    IsActive = p.IsActive,
                    DiscountLimit = p.DiscountLimit,
                    Season = p.Season,
                    TotalStock = inStockVariants.Sum(v => v.Stock),
                    Colors = inStockVariants.Select(v => v.Color).Distinct().ToList(),
                    Sizes = inStockVariants.Select(v => v.Size).Distinct().ToList(),
                    LastSoldAt = lastSoldMap.TryGetValue(p.Id, out var last) ? last : null,
                    Variants = inStockVariants.Select(v => new ProductVariantViewModel
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Stock = v.Stock
                    }).ToList()
                };
            }).ToList();

            return Json(result);
        }

        // POST: /Products/Add
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid product data");

            try
            {
                // Server-side validations
                if (model.SellingPrice < model.BuyingPrice)
                    return BadRequest(new { message = "Selling price cannot be lower than buying price." });

                if (!string.IsNullOrWhiteSpace(model.Code))
                {
                    var exists = await _productService.CodeExistsAsync(model.Code.Trim(), null);
                    if (exists) return BadRequest(new { message = "Product code already exists." });
                }

                if (model.DiscountLimit.HasValue && model.SellingPrice > 0)
                {
                    var allowed = Math.Max(0m, ((model.SellingPrice - model.BuyingPrice) / model.SellingPrice) * 100m);
                    if (model.DiscountLimit.Value - allowed > 0.0001m)
                        return BadRequest(new { message = $"Discount exceeds maximum allowed ({Math.Round(allowed,2)}%)." });
                }

                var product = new Product
                {
                    Code = model.Code,
                    Description = model.Description ?? string.Empty,
                    VendorId = model.VendorId,
                    BuyingPrice = model.BuyingPrice,
                    SellingPrice = model.SellingPrice,
                    IsActive = model.IsActive,
                    DiscountLimit = model.DiscountLimit,
                    Season = model.Season
                };

                var variants = (model.Variants ?? new List<ProductVariantViewModel>())
                    .Select(v => new ProductVariant
                    {
                        Color = v.Color ?? string.Empty,
                        Size = v.Size,
                        Stock = v.Stock
                    }).ToList();

                var added = await _productService.AddAsync(product, variants);
                if (added is null) return BadRequest("Could not add product");

                var result = new ProductViewModel
                {
                    Id = added.Id,
                    Code = added.Code,
                    Description = added.Description,
                    VendorId = added.VendorId,
                    VendorName = added.Vendor?.Name,
                    BuyingPrice = added.BuyingPrice,
                    SellingPrice = added.SellingPrice,
                    IsActive = added.IsActive,
                    DiscountLimit = added.DiscountLimit,
                    Season = added.Season,
                    TotalStock = added.Variants?.Sum(v => v.Stock) ?? 0,
                    Colors = added.Variants?.Select(v => v.Color).Distinct().ToList() ?? new List<string>(),
                    Sizes = added.Variants?.Select(v => v.Size).Distinct().ToList() ?? new List<int>(),
                    Variants = added.Variants?.Select(v => new ProductVariantViewModel
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Stock = v.Stock
                    }).ToList() ?? new List<ProductVariantViewModel>()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: /Products/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] ProductViewModel model)
        {
            if (!ModelState.IsValid || model.Id <= 0)
                return BadRequest("Invalid product data");

            try
            {
                // Server-side validations
                if (model.SellingPrice < model.BuyingPrice)
                    return BadRequest(new { message = "Selling price cannot be lower than buying price." });

                if (!string.IsNullOrWhiteSpace(model.Code))
                {
                    var exists = await _productService.CodeExistsAsync(model.Code.Trim(), model.Id);
                    if (exists) return BadRequest(new { message = "Product code already exists." });
                }

                if (model.DiscountLimit.HasValue && model.SellingPrice > 0)
                {
                    var allowed = Math.Max(0m, ((model.SellingPrice - model.BuyingPrice) / model.SellingPrice) * 100m);
                    if (model.DiscountLimit.Value - allowed > 0.0001m)
                        return BadRequest(new { message = $"Discount exceeds maximum allowed ({Math.Round(allowed,2)}%)." });
                }

                var product = new Product
                {
                    Id = model.Id,
                    Code = model.Code,
                    Description = model.Description ?? string.Empty,
                    VendorId = model.VendorId,
                    BuyingPrice = model.BuyingPrice,
                    SellingPrice = model.SellingPrice,
                    IsActive = model.IsActive,
                    DiscountLimit = model.DiscountLimit,
                    Season = model.Season
                };

                var variants = (model.Variants ?? new List<ProductVariantViewModel>())
                    .Select(v => new ProductVariant
                    {
                        Id = v.Id, // Include existing variant ID
                        Color = v.Color ?? string.Empty,
                        Size = v.Size,
                        Stock = v.Stock
                    }).ToList();

                var updated = await _productService.UpdateAsync(product, variants);
                if (updated is null) return NotFound();

                var result = new ProductViewModel
                {
                    Id = updated.Id,
                    Code = updated.Code,
                    Description = updated.Description,
                    VendorId = updated.VendorId,
                    VendorName = updated.Vendor?.Name,
                    BuyingPrice = updated.BuyingPrice,
                    SellingPrice = updated.SellingPrice,
                    IsActive = updated.IsActive,
                    DiscountLimit = updated.DiscountLimit,
                    Season = updated.Season,
                    TotalStock = updated.Variants?.Sum(v => v.Stock) ?? 0,
                    Colors = updated.Variants?.Select(v => v.Color).Distinct().ToList() ?? new List<string>(),
                    Sizes = updated.Variants?.Select(v => v.Size).Distinct().ToList() ?? new List<int>(),
                    Variants = updated.Variants?.Select(v => new ProductVariantViewModel
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Stock = v.Stock
                    }).ToList() ?? new List<ProductVariantViewModel>()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: /Products/Delete/{id}
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _productService.DeleteAsync(id);
                return ok ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: /Products/UpdatePrices/{id}
        [HttpPut]
        public async Task<IActionResult> UpdatePrices(int id, [FromBody] UpdatePricesRequest request)
        {
            try
            {
                var product = await _productService.GetByIdWithDetailsAsync(id);
                if (product == null)
                    return NotFound();

                product.BuyingPrice = request.BuyingPrice;
                product.SellingPrice = request.SellingPrice;
                product.DiscountLimit = request.DiscountLimit;

                await _productService.UpdateAsync(product, product.Variants);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product prices");
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: /Products/ToggleActive/{id}?isActive=true
        [HttpPost]
        [Route("Products/ToggleActive/{id}")]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            try
            {
                var ok = await _productService.SetActiveAsync(id, isActive);
                return ok ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product active state");
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: /Products/ToggleSeasonActive?season=1&isActive=true
        [HttpPost]
        [Route("Products/ToggleSeasonActive")]
        public async Task<IActionResult> ToggleSeasonActive([FromQuery] int season, [FromQuery] bool isActive)
        {
            try
            {
                _logger.LogInformation($"ToggleSeasonActive called with season={season}, isActive={isActive}");
                
                var products = await _productService.GetAllWithDetailsAsync();
                _logger.LogInformation($"Total products fetched: {products.Count}");
                
                var seasonProducts = products.Where(p => (int)p.Season == season).ToList();
                _logger.LogInformation($"Products matching season {season}: {seasonProducts.Count}");

                int count = 0;
                foreach (var product in seasonProducts)
                {
                    _logger.LogInformation($"Processing Product ID={product.Id}, Code={product.Code}, Current IsActive={product.IsActive}, Target IsActive={isActive}");
                    
                    // Only update if the status is different
                    if (product.IsActive != isActive)
                    {
                        var success = await _productService.SetActiveAsync(product.Id, isActive);
                        if (success)
                        {
                            count++;
                            _logger.LogInformation($"Successfully updated Product ID={product.Id}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to update Product ID={product.Id}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Product ID={product.Id} already has IsActive={isActive}, skipping");
                    }
                }

                _logger.LogInformation($"Total products updated: {count}");
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling products by season");
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class UpdatePricesRequest
    {
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountLimit { get; set; }
    }
}
