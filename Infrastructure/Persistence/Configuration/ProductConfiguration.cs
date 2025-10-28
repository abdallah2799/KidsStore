using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.BuyingPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.SellingPrice)
                .HasColumnType("decimal(18,2)");

            // Optional discount limit with explicit precision to avoid default mapping warnings
            builder.Property(p => p.DiscountLimit)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
