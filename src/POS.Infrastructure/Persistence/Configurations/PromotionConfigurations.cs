using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.Value).HasMoneyPrecision();
        builder.Property(p => p.MinOrderAmount).HasMoneyPrecision();
        builder.Property(p => p.MaxDiscountAmount).HasMoneyPrecision();
        builder.Property(p => p.AppliesTo).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(PromotionStatus.Active);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(p => new { p.StoreId, p.Status, p.ValidFrom, p.ValidTo });
        builder.HasOne(p => p.Store).WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CreatedByEmployee).WithMany().HasForeignKey(p => p.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_promotions_type", "type IN ('PercentSku','FixedSku','BuyXGetY','CartPercent','CartFixed','HappyHour')");
        builder.HasTableCheckConstraint("ck_promotions_value", "value >= 0");
        builder.HasTableCheckConstraint("ck_promotions_applies_to", "applies_to IN ('All','Category','SKU')");
        builder.HasTableCheckConstraint("ck_promotions_status", "status IN ('Active','Inactive')");
        builder.HasTableCheckConstraint("ck_promotions_valid_range", "valid_to IS NULL OR valid_to >= valid_from");
    }
}

public class PromotionTargetConfiguration : IEntityTypeConfiguration<PromotionTarget>
{
    public void Configure(EntityTypeBuilder<PromotionTarget> builder)
    {
        builder.ToTable("promotion_targets");
        builder.ConfigureUuidPrimaryKey();
        builder.HasOne(p => p.Promotion).WithMany().HasForeignKey(p => p.PromotionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Sku).WithMany().HasForeignKey(p => p.SkuId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_promotion_targets_exactly_one_target", "(category_id IS NOT NULL AND sku_id IS NULL) OR (category_id IS NULL AND sku_id IS NOT NULL)");
    }
}

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("vouchers");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.Property(v => v.PerCustomerLimit).HasDefaultValue(1);
        builder.Property(v => v.IsActive).HasDefaultValue(true);
        builder.HasIndex(v => v.Code).IsUnique();
        builder.HasOne(v => v.Promotion).WithMany().HasForeignKey(v => v.PromotionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_vouchers_max_uses", "max_uses > 0");
        builder.HasTableCheckConstraint("ck_vouchers_used_count", "used_count <= max_uses");
    }
}

public class VoucherUsageConfiguration : IEntityTypeConfiguration<VoucherUsage>
{
    public void Configure(EntityTypeBuilder<VoucherUsage> builder)
    {
        builder.ToTable("voucher_usages");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(v => v.UsedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(v => new { v.VoucherId, v.OrderId }).IsUnique();
        builder.HasOne(v => v.Voucher).WithMany().HasForeignKey(v => v.VoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Customer).WithMany().HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Order).WithMany().HasForeignKey(v => v.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
