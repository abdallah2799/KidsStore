using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration
{
    public class PurchaseReturnInvoiceConfiguration : IEntityTypeConfiguration<PurchaseReturnInvoice>
    {
        public void Configure(EntityTypeBuilder<PurchaseReturnInvoice> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TotalRefund)
                .HasPrecision(18, 2);

            builder.Property(p => p.ReturnDate)
                .IsRequired();

            builder.Property(p => p.Reason)
                .HasMaxLength(500);

            builder.HasOne(p => p.PurchaseInvoice)
                .WithMany()
                .HasForeignKey(p => p.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Items)
                .WithOne(i => i.PurchaseReturnInvoice)
                .HasForeignKey(i => i.PurchaseReturnInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
