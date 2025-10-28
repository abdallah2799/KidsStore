using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Apply any pending migrations first to ensure schema is up to date
            await context.Database.MigrateAsync();

            // Seed Users
            if (!await context.Users.AnyAsync())
            {
                var admin = new User { UserName = "admin", Role = UserRole.Admin, IsActive = true };
                admin.SetPassword("admin123");
                var cashier = new User { UserName = "cashier", Role = UserRole.Cashier, IsActive = true };
                cashier.SetPassword("cashier123");
                await context.Users.AddRangeAsync(admin, cashier);
                await context.SaveChangesAsync();
            }

            // Seed Vendors
            if (!await context.Vendors.AnyAsync())
            {
                var vendors = new List<Vendor>
                {
                    new Vendor { Name = "Vendor Alpha", CodePrefix = "VA", Address = "Cairo", ContactInfo = "01000000001", Notes = "Electronics" },
                    new Vendor { Name = "Vendor Beta", CodePrefix = "VB", Address = "Alexandria", ContactInfo = "01000000002", Notes = "Stationery" }
                };
                await context.Vendors.AddRangeAsync(vendors);
                await context.SaveChangesAsync();
            }

            // Seed Products & Variants
            if (!await context.Products.AnyAsync())
            {
                var vendor1 = await context.Vendors.FirstAsync();
                var vendor2 = await context.Vendors.Skip(1).FirstAsync();
                var products = new List<Product>
                {
                    new Product
                    {
                        Code = "P-001",
                        Description = "Kids T-Shirt",
                        VendorId = vendor1.Id,
                        BuyingPrice = 50,
                        SellingPrice = 80,
                        IsActive = true,
                        DiscountLimit = 5,
                        Season = Season.Summer,
                        Variants = new List<ProductVariant>
                        {
                            new ProductVariant { Color = "Red", Size = 6, Stock = 20 },
                            new ProductVariant { Color = "Green", Size = 8, Stock = 15 }
                        }
                    },
                    new Product
                    {
                        Code = "P-002",
                        Description = "School Backpack",
                        VendorId = vendor2.Id,
                        BuyingPrice = 90,
                        SellingPrice = 140,
                        IsActive = true,
                        DiscountLimit = 0,
                        Season = Season.Fall,
                        Variants = new List<ProductVariant>
                        {
                            new ProductVariant { Color = "Blue", Size = 10, Stock = 5 }
                        }
                    }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // Seed Purchases
            if (!await context.PurchaseInvoices.AnyAsync())
            {
                var vendor1 = await context.Vendors.FirstAsync();
                var productVariant = await context.ProductVariants.FirstAsync();
                var purchase = new PurchaseInvoice
                {
                    VendorId = vendor1.Id,
                    PurchaseDate = DateTime.Now.AddDays(-10),
                    TotalAmount = 1000,
                    Items = new List<PurchaseItem>
                    {
                        new PurchaseItem { ProductVariantId = productVariant.Id, Quantity = 10, BuyingPrice = 50 }
                    }
                };
                await context.PurchaseInvoices.AddAsync(purchase);
                await context.SaveChangesAsync();
            }

            // Seed Sales
            if (!await context.SalesInvoices.AnyAsync())
            {
                var admin = await context.Users.FirstAsync();
                var productVariant = await context.ProductVariants.FirstAsync();
                var sale = new SalesInvoice
                {
                    SellerId = admin.Id,
                    SaleDate = DateTime.Now.AddDays(-5),
                    TotalAmount = 200,
                    PaymentMethod = "Cash",
                    CustomerName = "Test Customer",
                    Items = new List<SalesItem>
                    {
                        new SalesItem { ProductVariantId = productVariant.Id, Quantity = 2, SellingPrice = 80, DiscountValue = 5 }
                    }
                };
                await context.SalesInvoices.AddAsync(sale);
                await context.SaveChangesAsync();
            }

            // Seed Returns
            if (!await context.ReturnInvoices.AnyAsync())
            {
                var sale = await context.SalesInvoices.FirstAsync();
                var productVariant = await context.ProductVariants.FirstAsync();
                var returnInvoice = new ReturnInvoice
                {
                    SalesInvoiceId = sale.Id,
                    ReturnDate = DateTime.Now.AddDays(-2),
                    TotalRefund = 80,
                    Items = new List<ReturnItem>
                    {
                        new ReturnItem { ProductVariantId = productVariant.Id, Quantity = 1, RefundAmount = 80 }
                    }
                };
                await context.ReturnInvoices.AddAsync(returnInvoice);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedVendorsAsync(AppDbContext context)
        {
            // Removed old vendor seeding
        }

        private static async Task SeedProductsAsync(AppDbContext context)
        {
            // Removed old product seeding
        }
    }
}
