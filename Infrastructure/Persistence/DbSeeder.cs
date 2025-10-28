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

            await SeedVendorsAsync(context);
            await SeedProductsAsync(context);
        }

        private static async Task SeedVendorsAsync(AppDbContext context)
        {
            if (await context.Vendors.AnyAsync()) return;

            var vendors = new List<Vendor>
            {
                new Vendor { Name = "المورد ألفا", CodePrefix = "MA", Address = "القاهرة، مصر", ContactInfo = "01000000001", Notes = "أفضل مورد للأجهزة الإلكترونية" },
                new Vendor { Name = "المورد بيتا", CodePrefix = "MB", Address = "الإسكندرية، مصر", ContactInfo = "01000000002", Notes = "مورد مستلزمات مكتبية" },
                new Vendor { Name = "المورد جاما", CodePrefix = "MG", Address = "الجيزة، مصر", ContactInfo = "01000000003", Notes = "لديه خبرة 10 سنوات" },
                new Vendor { Name = "المورد دلتا", CodePrefix = "MD", Address = "طنطا، مصر", ContactInfo = "01000000004", Notes = "" },
                new Vendor { Name = "المورد إبسلون", CodePrefix = "ME", Address = "منصورة، مصر", ContactInfo = "01000000005", Notes = "سريع في التسليم" },
                new Vendor { Name = "المورد زيتا", CodePrefix = "MZ", Address = "الإسماعيلية، مصر", ContactInfo = "01000000006", Notes = "" },
                new Vendor { Name = "المورد إيتا", CodePrefix = "MET", Address = "السويس، مصر", ContactInfo = "01000000007", Notes = "" },
                new Vendor { Name = "المورد ثيتا", CodePrefix = "MTH", Address = "شرم الشيخ، مصر", ContactInfo = "01000000008", Notes = "مورد سياحي وتجهيز الفنادق" },
                new Vendor { Name = "المورد يوتا", CodePrefix = "MI", Address = "الغردقة، مصر", ContactInfo = "01000000009", Notes = "" },
                new Vendor { Name = "المورد كابا", CodePrefix = "MK", Address = "الأقصر، مصر", ContactInfo = "01000000010", Notes = "يقدم خصومات عند الشراء بالجملة" }
            };

            await context.Vendors.AddRangeAsync(vendors);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductsAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            var vendors = await context.Vendors.OrderBy(v => v.Id).ToListAsync();
            if (vendors.Count == 0) return; // Vendors should be seeded first

            int v1 = vendors[0].Id;
            int v2 = vendors.Count > 1 ? vendors[1].Id : vendors[0].Id;

            var products = new List<Product>
            {
                new Product
                {
                    Code = "P-0001",
                    Description = "Kids T-Shirt",
                    VendorId = v1,
                    BuyingPrice = 50,
                    SellingPrice = 80,
                    IsActive = true,
                    DiscountLimit = 5,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Color = "#ff4d4d", Size = 6, Stock = 20 },
                        new ProductVariant { Color = "#22c55e", Size = 8, Stock = 15 },
                        new ProductVariant { Color = "#3b82f6", Size = 10, Stock = 12 }
                    }
                },
                new Product
                {
                    Code = "P-0002",
                    Description = "Kids Sneakers",
                    VendorId = v2,
                    BuyingPrice = 120,
                    SellingPrice = 180,
                    IsActive = true,
                    DiscountLimit = 10,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Color = "#111827", Size = 12, Stock = 10 },
                        new ProductVariant { Color = "#f59e0b", Size = 14, Stock = 8 }
                    }
                },
                new Product
                {
                    Code = "P-0003",
                    Description = "School Backpack",
                    VendorId = v1,
                    BuyingPrice = 90,
                    SellingPrice = 140,
                    IsActive = true,
                    DiscountLimit = 0,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Color = "#0ea5e9", Size = 10, Stock = 5 },
                        new ProductVariant { Color = "#a855f7", Size = 12, Stock = 2 }
                    }
                },
                new Product
                {
                    Code = "P-0004",
                    Description = "Baby Onesie",
                    VendorId = v2,
                    BuyingPrice = 30,
                    SellingPrice = 55,
                    IsActive = true,
                    DiscountLimit = 3,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Color = "#fca5a5", Size = 2, Stock = 25 },
                        new ProductVariant { Color = "#fde68a", Size = 4, Stock = 18 }
                    }
                },
                new Product
                {
                    Code = "P-0005",
                    Description = "Kids Hat",
                    VendorId = v1,
                    BuyingPrice = 20,
                    SellingPrice = 40,
                    IsActive = false,
                    DiscountLimit = null,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Color = "#f97316", Size = 6, Stock = 3 }
                    }
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
