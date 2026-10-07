using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Configurations;

public class PosRegisterConfiguration : IEntityTypeConfiguration<PosRegister>
{
    public void Configure(EntityTypeBuilder<PosRegister> builder)
    {
        builder.ToTable("pos_registers");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(r => new { r.StoreId, r.Code }).IsUnique();
        builder.HasOne(r => r.Store).WithMany().HasForeignKey(r => r.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}
