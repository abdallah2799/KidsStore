using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.ViewModels
{
    public class ProductVariantViewModel
    {
        public int Id { get; set; }
        [Required]
        public string Color { get; set; } = string.Empty;
        [Range(1, 18)]
        public int Size { get; set; }
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }
    }
}
