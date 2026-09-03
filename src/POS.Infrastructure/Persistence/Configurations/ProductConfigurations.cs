using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Products;
using POS.Domain.Products.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ImageUrl).HasMaxLength(500);
        builder.Property(c => c.IsVisible).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasOne(c => c.Store).WithMany().HasForeignKey(c => c.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Parent).WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.BaseUnit).IsRequired().HasMaxLength(30);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(ProductStatus.Active);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(p => new { p.StoreId, p.Status }).HasFilter("status = 'Active'");
        builder.HasOne(p => p.Store).WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_products_status", "status IN ('Active','Inactive')");
    }
}

public class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(s => s.SkuCode).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Barcode).IsRequired().HasMaxLength(50);
        builder.Property(s => s.CostPrice).HasMoneyPrecision();
        builder.Property(s => s.SellPrice).HasMoneyPrecision();
        builder.Property(s => s.TaxRate).HasPrecision(5, 2).HasDefaultValue(0);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(s => new { s.StoreId, s.Barcode }).IsUnique();
        builder.HasIndex(s => new { s.StoreId, s.SkuCode }).IsUnique();
        builder.HasIndex(s => new { s.StoreId, s.IsActive }).HasFilter("is_active = 1");
        builder.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_skus_cost_price", "cost_price >= 0");
        builder.HasTableCheckConstraint("ck_skus_sell_price", "sell_price >= 0");
        builder.HasTableCheckConstraint("ck_skus_tax_rate", "tax_rate IN (0,5,8,10)");
    }
}

public class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.ToTable("unit_conversions");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(u => u.UnitName).IsRequired().HasMaxLength(30);
        builder.Property(u => u.ConversionFactor).HasPrecision(18, 4);
        builder.Property(u => u.SellPrice).HasMoneyPrecision();
        builder.HasIndex(u => new { u.SkuId, u.UnitName }).IsUnique();
        builder.HasOne(u => u.Sku).WithMany().HasForeignKey(u => u.SkuId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_unit_conversions_conversion_factor", "conversion_factor > 0");
        builder.HasTableCheckConstraint("ck_unit_conversions_sell_price", "sell_price >= 0");
    }
}

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Price).HasMoneyPrecision();
        builder.Property(p => p.CustomerGroup).HasMaxLength(50);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasOne(p => p.Store).WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Sku).WithMany().HasForeignKey(p => p.SkuId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.CreatedByEmployee).WithMany().HasForeignKey(p => p.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_price_lists_price", "price >= 0");
        builder.HasTableCheckConstraint("ck_price_lists_valid_range", "valid_to IS NULL OR valid_to >= valid_from");
    }
}
