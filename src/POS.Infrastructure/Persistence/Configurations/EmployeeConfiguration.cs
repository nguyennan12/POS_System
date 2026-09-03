using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Employees;

namespace POS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Username).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(e => e.PinHash).IsRequired().HasMaxLength(255);
        builder.Property(e => e.PinLookupHash).HasColumnType("char(64)");
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => new { e.StoreId, e.PinLookupHash }).IsUnique();
        builder.HasOne(e => e.Role).WithMany(r => r.Employees).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_employees_store_required_unless_chain_owner", "store_id IS NOT NULL OR is_chain_owner = 1");
    }
}
