using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Size { get; set; } // 1–18
        public int Stock { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
    }
}
