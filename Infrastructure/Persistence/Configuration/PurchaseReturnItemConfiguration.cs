using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration
{
    public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Quantity)
                .IsRequired();

            builder.Property(p => p.RefundPrice)
                .HasPrecision(18, 2);

            builder.HasOne(p => p.PurchaseReturnInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(p => p.PurchaseReturnInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.ProductVariant)
                .WithMany()
                .HasForeignKey(p => p.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
