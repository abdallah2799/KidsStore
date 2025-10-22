using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Product : BaseEntity
    {
        public int VendorId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? DiscountLimit { get; set; }

        // Navigation
        public virtual Vendor Vendor { get; set; } = null!;
        public virtual ICollection<ProductVariant> Variants { get; set; } = [];
    }
}
