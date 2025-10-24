using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Vendor : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CodePrefix { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public virtual ICollection<Product> Products { get; set; }= new List<Product>();
    }
}
