using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.ViewModels
{
    public class VendorViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        public string CodePrefix { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactInfo { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
