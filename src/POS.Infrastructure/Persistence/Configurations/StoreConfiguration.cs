using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Timezone).IsRequired().HasMaxLength(50).HasDefaultValue("Asia/Ho_Chi_Minh");
        builder.Property(s => s.CurrencyCode).IsRequired().HasMaxLength(3).HasDefaultValue("VND");
        builder.Property(s => s.TaxCode).HasMaxLength(20);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasMany(s => s.Employees).WithOne(e => e.Store).HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}
