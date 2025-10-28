using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ReturnInvoiceConfiguration : IEntityTypeConfiguration<ReturnInvoice>
    {
        public void Configure(EntityTypeBuilder<ReturnInvoice> builder)
        {
            builder.ToTable("ReturnInvoices");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.TotalRefund)
                .HasColumnType("decimal(18,2)");

            builder.HasMany(r => r.Items)
                .WithOne(i => i.ReturnInvoice)
                .HasForeignKey(i => i.ReturnInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
