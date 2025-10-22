using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion(
                         v => v.ToString(),                  // convert enum to string for DB
                         v => (UserRole)Enum.Parse(typeof(UserRole), v) // convert string back to enum
                     )
                .HasMaxLength(20);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);
        }
    }
}
