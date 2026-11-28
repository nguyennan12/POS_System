using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Configurations;

public sealed class ScannerDeviceConfiguration : IEntityTypeConfiguration<ScannerDevice>
{
    public void Configure(EntityTypeBuilder<ScannerDevice> builder)
    {
        builder.ToTable("scanner_devices");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SecretHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => new { x.StoreId, x.RegisterId });
        builder.HasOne<Store>().WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PosRegister>().WithMany().HasForeignKey(x => x.RegisterId).OnDelete(DeleteBehavior.Restrict);
    }
}
