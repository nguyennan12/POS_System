using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class MemberTierConfiguration : IEntityTypeConfiguration<MemberTier>
{
    public void Configure(EntityTypeBuilder<MemberTier> builder)
    {
        builder.ToTable("member_tiers");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(m => m.Name).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(m => m.MinSpending).HasMoneyPrecision();
        builder.Property(m => m.PointRate).HasRatePrecision();
        builder.Property(m => m.DiscountRate).HasRatePrecision();
        builder.Property(m => m.DisplayColor).HasMaxLength(20);
        builder.HasIndex(m => m.Name).IsUnique();
        builder.HasTableCheckConstraint("ck_member_tiers_name", "name IN ('Normal','Silver','Gold','VIP')");
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Barcode).HasMaxLength(50);
        builder.Property(c => c.TotalSpending).HasMoneyPrecision();
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(c => c.Phone).IsUnique();
        builder.HasIndex(c => c.Barcode).IsUnique();
        builder.HasOne(c => c.MemberTier).WithMany().HasForeignKey(c => c.MemberTierId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("loyalty_accounts");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(l => l.PointsBalance).HasMoneyPrecision();
        builder.Property(l => l.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(l => l.CustomerId).IsUnique();
        builder.HasOne(l => l.Customer).WithOne().HasForeignKey<LoyaltyAccount>(l => l.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_loyalty_accounts_points_balance", "points_balance >= 0");
    }
}

public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.ToTable("point_transactions");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Points).HasMoneyPrecision();
        builder.Property(p => p.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(p => new { p.CustomerId, p.CreatedAt });
        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Order).WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_point_transactions_type", "type IN ('Earn','Redeem','Adjust')");
    }
}
