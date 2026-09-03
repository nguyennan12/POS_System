using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(o => o.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(OrderStatus.Draft);
        builder.Property(o => o.CurrencyCode).IsRequired().HasMaxLength(3).HasDefaultValue("VND");
        builder.Property(o => o.Subtotal).HasMoneyPrecision();
        builder.Property(o => o.DiscountTotal).HasMoneyPrecision();
        builder.Property(o => o.TaxTotal).HasMoneyPrecision();
        builder.Property(o => o.GrandTotal).HasMoneyPrecision();
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(o => new { o.StoreId, o.CreatedAt });
        builder.HasIndex(o => o.ShiftId);
        builder.HasOne(o => o.Store).WithMany().HasForeignKey(o => o.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Shift).WithMany().HasForeignKey(o => o.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.CreatedByEmployee).WithMany().HasForeignKey(o => o.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_orders_status", "status IN ('Draft','Confirmed','Paid','Cancelled')");
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(o => o.Qty).HasQuantityPrecision();
        builder.Property(o => o.UnitPrice).HasMoneyPrecision();
        builder.Property(o => o.DiscountAmount).HasMoneyPrecision();
        builder.Property(o => o.TaxAmount).HasMoneyPrecision();
        builder.Property(o => o.LineTotal).HasMoneyPrecision();
        builder.HasOne(o => o.Order).WithMany(o => o.Items).HasForeignKey(o => o.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Sku).WithMany().HasForeignKey(o => o.SkuId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_order_items_qty", "qty > 0");
        builder.HasTableCheckConstraint("ck_order_items_unit_price", "unit_price >= 0");
    }
}

public class OrderDiscountConfiguration : IEntityTypeConfiguration<OrderDiscount>
{
    public void Configure(EntityTypeBuilder<OrderDiscount> builder)
    {
        builder.ToTable("order_discounts");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(o => o.DiscountAmount).HasMoneyPrecision();
        builder.Property(o => o.Description).HasMaxLength(300);
        builder.Property(o => o.AppliedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasOne(o => o.Order).WithMany().HasForeignKey(o => o.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Promotion).WithMany().HasForeignKey(o => o.PromotionId).OnDelete(DeleteBehavior.ClientSetNull);
        builder.HasOne(o => o.Voucher).WithMany().HasForeignKey(o => o.VoucherId).OnDelete(DeleteBehavior.ClientSetNull);
        builder.HasTableCheckConstraint("ck_order_discounts_amount", "discount_amount >= 0");
        builder.HasTableCheckConstraint("ck_order_discounts_source", "promotion_id IS NOT NULL OR voucher_id IS NOT NULL");
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(p => p.Method).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.Amount).HasMoneyPrecision();
        builder.Property(p => p.ChangeAmount).HasMoneyPrecision();
        builder.Property(p => p.TransactionRef).HasMaxLength(100);
        builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(PaymentStatus.Pending);
        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => new { p.Method, p.TransactionRef }).IsUnique();
        builder.HasOne(p => p.Order).WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasTableCheckConstraint("ck_payments_method", "method IN ('Cash','MoMo','VietQR','Card','Points')");
        builder.HasTableCheckConstraint("ck_payments_amount", "amount > 0");
        builder.HasTableCheckConstraint("ck_payments_change_amount", "change_amount IS NULL OR change_amount >= 0");
        builder.HasTableCheckConstraint("ck_payments_status", "status IN ('Pending','Success','Failed','Timeout')");
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(i => i.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.Property(i => i.BuyerName).HasMaxLength(200);
        builder.Property(i => i.BuyerTaxCode).HasMaxLength(20);
        builder.Property(i => i.BuyerAddress).HasMaxLength(500);
        builder.Property(i => i.TotalBeforeTax).HasMoneyPrecision();
        builder.Property(i => i.TaxAmount).HasMoneyPrecision();
        builder.Property(i => i.GrandTotal).HasMoneyPrecision();
        builder.Property(i => i.IssuedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(i => i.OrderId).IsUnique();
        builder.HasIndex(i => i.InvoiceNo).IsUnique();
        builder.HasOne(i => i.Order).WithOne().HasForeignKey<Invoice>(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
