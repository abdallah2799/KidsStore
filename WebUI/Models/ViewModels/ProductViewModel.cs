using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace WebUI.Models.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int VendorId { get; set; }

        public string? VendorName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BuyingPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public decimal? DiscountLimit { get; set; }
        
        public Season Season { get; set; } = Season.AllYear;

        // Aggregates
        public int TotalStock { get; set; }
        public List<string> Colors { get; set; } = new();
        public List<int> Sizes { get; set; } = new();

        public List<ProductVariantViewModel> Variants { get; set; } = new();

        // Insights
        public DateTime? LastSoldAt { get; set; }
    }
}
