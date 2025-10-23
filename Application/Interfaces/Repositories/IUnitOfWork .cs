using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Product> Products { get; }
        IRepository<PurchaseInvoice> PurchaseInvoices { get; }

        IRepository<T> Repository<T>() where T : class;
        // ... add others as needed

        Task<int> SaveChangesAsync();
    }
}
