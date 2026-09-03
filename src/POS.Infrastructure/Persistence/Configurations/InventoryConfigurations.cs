using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Inventory.StockIn;
using POS.Domain.Inventory.StockTake;
using POS.Domain.Inventory.Suppliers;

namespace POS.Infrastructure.Persistence.Configurations;

public class StockEntryConfiguration : IEntityTypeConfiguration<StockEntry>
{
  public void Configure(EntityTypeBuilder<StockEntry> builder)
  {
    builder.ToTable("stock_entries");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.QtyOnHand).HasQuantityPrecision();
    builder.Property(s => s.MinStock).HasQuantityPrecision();
    builder.Property(s => s.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
    builder.HasIndex(s => new { s.StoreId, s.SkuId }).IsUnique();
    builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Sku).WithMany().HasForeignKey(s => s.SkuId).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_entries_qty_on_hand", "qty_on_hand >= 0");
  }
}

public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
  public void Configure(EntityTypeBuilder<StockBatch> builder)
  {
    builder.ToTable("stock_batches");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.BatchNo).IsRequired().HasMaxLength(50);
    builder.Property(s => s.Qty).HasQuantityPrecision();
    builder.Property(s => s.ReceivedAt).HasDefaultValueSql("GETUTCDATE()");
    builder.HasIndex(s => new { s.StoreId, s.SkuId, s.BatchNo }).IsUnique();
    builder.HasIndex(s => s.ExpiryDate);
    builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Sku).WithMany().HasForeignKey(s => s.SkuId).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_batches_qty", "qty >= 0");
  }
}

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
  public void Configure(EntityTypeBuilder<StockTransaction> builder)
  {
    builder.ToTable("stock_transactions");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
    builder.Property(s => s.Qty).HasQuantityPrecision();
    builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    builder.HasIndex(s => new { s.SkuId, s.CreatedAt });
    builder.HasIndex(s => new { s.StoreId, s.CreatedAt });
    builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Sku).WithMany().HasForeignKey(s => s.SkuId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Order).WithMany().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.StockInVoucher).WithMany().HasForeignKey(s => s.StockInVoucherId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.CreatedByEmployee).WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_transactions_type", "type IN ('StockIn','SaleOut','Dispose','Adjust')");
  }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
  public void Configure(EntityTypeBuilder<Supplier> builder)
  {
    builder.ToTable("suppliers");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
    builder.Property(s => s.TaxCode).HasMaxLength(20);
    builder.Property(s => s.ContactName).HasMaxLength(100);
    builder.Property(s => s.Phone).HasMaxLength(20);
    builder.Property(s => s.Email).HasMaxLength(100);
    builder.Property(s => s.Address).HasMaxLength(500);
    builder.Property(s => s.CreditTerms).HasMaxLength(200);
    builder.Property(s => s.IsActive).HasDefaultValue(true);
    builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
  }
}

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
  public void Configure(EntityTypeBuilder<SupplierPayment> builder)
  {
    builder.ToTable("supplier_payments");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.Amount).HasMoneyPrecision();
    builder.Property(s => s.Method).HasConversion<string>().IsRequired().HasMaxLength(20);
    builder.Property(s => s.PaidAt).HasDefaultValueSql("GETUTCDATE()");
    builder.HasOne(s => s.Supplier).WithMany().HasForeignKey(s => s.SupplierId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Voucher).WithMany().HasForeignKey(s => s.VoucherId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.CreatedByEmployee).WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_supplier_payments_amount", "amount > 0");
    builder.HasTableCheckConstraint("ck_supplier_payments_method", "method IN ('Cash','BankTransfer','Other')");
  }
}

public class StockInVoucherConfiguration : IEntityTypeConfiguration<StockInVoucher>
{
  public void Configure(EntityTypeBuilder<StockInVoucher> builder)
  {
    builder.ToTable("stock_in_vouchers");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.TotalAmount).HasMoneyPrecision();
    builder.Property(s => s.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(StockInVoucherStatus.Completed);
    builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.Supplier).WithMany().HasForeignKey(s => s.SupplierId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.CreatedByEmployee).WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_in_vouchers_total_amount", "total_amount >= 0");
    builder.HasTableCheckConstraint("ck_stock_in_vouchers_status", "status IN ('Draft','Completed','Cancelled')");
  }
}

public class StockInVoucherItemConfiguration : IEntityTypeConfiguration<StockInVoucherItem>
{
  public void Configure(EntityTypeBuilder<StockInVoucherItem> builder)
  {
    builder.ToTable("stock_in_voucher_items");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.Qty).HasQuantityPrecision();
    builder.Property(s => s.UnitPrice).HasMoneyPrecision();
    builder.Property(s => s.TotalPrice).HasMoneyPrecision();
    builder.HasOne(s => s.Voucher).WithMany(v => v.Items).HasForeignKey(s => s.VoucherId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne(s => s.Sku).WithMany().HasForeignKey(s => s.SkuId).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_in_voucher_items_qty", "qty > 0");
    builder.HasTableCheckConstraint("ck_stock_in_voucher_items_unit_price", "unit_price >= 0");
    builder.HasTableCheckConstraint("ck_stock_in_voucher_items_total_price", "total_price >= 0");
  }
}

public class StockTakeConfiguration : IEntityTypeConfiguration<StockTake>
{
  public void Configure(EntityTypeBuilder<StockTake> builder)
  {
    builder.ToTable("stock_takes");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.Status).HasConversion<string>().IsRequired().HasMaxLength(20).HasDefaultValue(StockTakeStatus.Draft);
    builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.CreatedByEmployee).WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(s => s.ApprovedByEmployee).WithMany().HasForeignKey(s => s.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
    builder.HasTableCheckConstraint("ck_stock_takes_status", "status IN ('Draft','Pending','Approved')");
  }
}

public class StockTakeItemConfiguration : IEntityTypeConfiguration<StockTakeItem>
{
  public void Configure(EntityTypeBuilder<StockTakeItem> builder)
  {
    builder.ToTable("stock_take_items");
    builder.ConfigureUuidPrimaryKey();
    builder.Property(s => s.SystemQty).HasQuantityPrecision();
    builder.Property(s => s.ActualQty).HasQuantityPrecision();
    builder.Property(s => s.DiffQty).HasQuantityPrecision().HasComputedColumnSql("actual_qty - system_qty", stored: true);
    builder.HasOne(s => s.Take).WithMany(t => t.Items).HasForeignKey(s => s.TakeId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne(s => s.Sku).WithMany().HasForeignKey(s => s.SkuId).OnDelete(DeleteBehavior.Restrict);
  }
}
