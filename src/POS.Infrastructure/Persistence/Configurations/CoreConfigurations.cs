using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class EmployeeStoreAccessConfiguration : IEntityTypeConfiguration<EmployeeStoreAccess>
{
    public void Configure(EntityTypeBuilder<EmployeeStoreAccess> builder)
    {
        builder.ToTable("employee_store_access");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(e => e.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(e => new { e.EmployeeId, e.StoreId }).IsUnique();
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Store).WithMany().HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.GrantedByEmployee).WithMany().HasForeignKey(e => e.GrantedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(255);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => new { r.EmployeeId, r.RevokedAt });
        builder.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(s => s.OpeningCash).HasMoneyPrecision();
        builder.Property(s => s.ClosingCash).HasMoneyPrecision();
        builder.Property(s => s.ActualCash).HasMoneyPrecision();
        builder.Property(s => s.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(ShiftStatus.Open);
        builder.Property(s => s.OpenedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(s => new { s.StoreId, s.Status });
        builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Employee).WithMany().HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_shifts_opening_cash", "opening_cash >= 0");
        builder.HasTableCheckConstraint("ck_shifts_closing_cash", "closing_cash IS NULL OR closing_cash >= 0");
        builder.HasTableCheckConstraint("ck_shifts_actual_cash", "actual_cash IS NULL OR actual_cash >= 0");
        builder.HasTableCheckConstraint("ck_shifts_status", "status IN ('Open','Closed')");
    }
}
