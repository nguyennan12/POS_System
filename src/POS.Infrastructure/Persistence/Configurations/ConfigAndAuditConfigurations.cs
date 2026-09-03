using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Auditing;
using POS.Domain.Configuration;

namespace POS.Infrastructure.Persistence.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(s => new { s.StoreId, s.Key }).IsUnique();
        builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("translations");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(t => t.Key).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Value).IsRequired();
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(t => new { t.LanguageCode, t.Key }).IsUnique();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => new { a.EmployeeId, a.CreatedAt });
        builder.HasOne(a => a.Store).WithMany().HasForeignKey(a => a.StoreId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.SetNull);
    }
}
