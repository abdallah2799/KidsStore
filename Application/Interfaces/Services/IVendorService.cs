using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVendorService
    {
        Task<List<Vendor>> GetAllAsync();
        Task<Vendor?> AddAsync(Vendor vendor);
        Task<Vendor?> UpdateAsync(Vendor vendor);
        Task<bool> DeleteAsync(int id);
        Task<bool> IsNameExistsAsync(string name);
    }
}
