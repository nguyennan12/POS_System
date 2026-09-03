using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.IsSystemRole).HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(r => new { r.StoreId, r.Name }).IsUnique();
        builder.HasOne(r => r.Store).WithMany().HasForeignKey(r => r.StoreId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasMaxLength(200);
        builder.HasIndex(r => r.Code).IsUnique();
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Action).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Description).HasMaxLength(200);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => new { p.ResourceId, p.Action }).IsUnique();
        builder.HasOne(p => p.Resource).WithMany(r => r.Permissions).HasForeignKey(p => p.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_permissions_action", "action IN ('Create','Read','Update','Delete','Approve','Export','Override')");
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(rp => rp.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Permission).WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.GrantedByEmployee).WithMany().HasForeignKey(rp => rp.GrantedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
