using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class VendorService : IVendorService
    {
        private readonly IUnitOfWork _uow;

        public VendorService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<Vendor>> GetAllAsync()
        {
            return await _uow.Repository<Vendor>().AsQueryable().ToListAsync();
        }

        public async Task<Vendor?> AddAsync(Vendor vendor)
        {
            var repo = _uow.Repository<Vendor>();

            var exists = await repo.AsQueryable().AnyAsync(v => v.Name == vendor.Name);
            if (exists)
                throw new InvalidOperationException("Vendor name already exists.");

            await repo.AddAsync(vendor);
            await _uow.SaveChangesAsync();
            return vendor;
        }

        public async Task<Vendor?> UpdateAsync(Vendor vendor)
        {
            var repo = _uow.Repository<Vendor>();
            var existing = await repo.GetByIdAsync(vendor.Id);

            if (existing is null)
                return null;

            existing.Name = vendor.Name;
            existing.Address = vendor.Address;
            existing.Notes = vendor.Notes;
            existing.CodePrefix = vendor.CodePrefix;
            existing.ContactInfo = vendor.ContactInfo;

            await _uow.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var repo = _uow.Repository<Vendor>();
            var vendor = await repo.GetByIdAsync(id);
            if (vendor is null)
                return false;

            repo.Remove(vendor);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsNameExistsAsync(string name)
        {
            var repo = _uow.Repository<Vendor>();
            return await repo.AsQueryable().AnyAsync(v => v.Name.ToLower() == name.ToLower());
        }
    }
}
