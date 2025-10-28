using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IRepository<User> Users => Repository<User>();
        public IRepository<Product> Products => Repository<Product>();
        public IRepository<PurchaseInvoice> PurchaseInvoices => Repository<PurchaseInvoice>();
        public IRepository<SalesInvoice> SalesInvoices => Repository<SalesInvoice>();

        public IRepository<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var repo))
                return (IRepository<T>)repo;

            var newRepo = new Repository<T>(_context);
            _repositories[typeof(T)] = newRepo;
            return newRepo;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
