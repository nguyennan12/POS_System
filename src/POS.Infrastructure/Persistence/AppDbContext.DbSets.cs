using Microsoft.EntityFrameworkCore;
using POS.Domain.Auditing;
using POS.Domain.Chatbot;
using POS.Domain.Configuration;
using POS.Domain.Customers;
using POS.Domain.Employees;
using POS.Domain.Inventory.Stock;
using POS.Domain.Inventory.StockIn;
using POS.Domain.Inventory.StockTake;
using POS.Domain.Inventory.Suppliers;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Rbac;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence;

public partial class AppDbContext
{
  public DbSet<Store> Stores => Set<Store>();
  public DbSet<Employee> Employees => Set<Employee>();
  public DbSet<EmployeeStoreAccess> EmployeeStoreAccesses => Set<EmployeeStoreAccess>();
  public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
  public DbSet<Shift> Shifts => Set<Shift>();

  public DbSet<Role> Roles => Set<Role>();
  public DbSet<Resource> Resources => Set<Resource>();
  public DbSet<Permission> Permissions => Set<Permission>();
  public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

  public DbSet<Category> Categories => Set<Category>();
  public DbSet<Product> Products => Set<Product>();
  public DbSet<Sku> Skus => Set<Sku>();
  public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
  public DbSet<PriceList> PriceLists => Set<PriceList>();

  public DbSet<StockEntry> StockEntries => Set<StockEntry>();
  public DbSet<StockBatch> StockBatches => Set<StockBatch>();
  public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
  public DbSet<Supplier> Suppliers => Set<Supplier>();
  public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
  public DbSet<StockInVoucher> StockInVouchers => Set<StockInVoucher>();
  public DbSet<StockInVoucherItem> StockInVoucherItems => Set<StockInVoucherItem>();
  public DbSet<StockTake> StockTakes => Set<StockTake>();
  public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();

  public DbSet<MemberTier> MemberTiers => Set<MemberTier>();
  public DbSet<Customer> Customers => Set<Customer>();
  public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
  public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();

  public DbSet<FaqEntry> FaqEntries => Set<FaqEntry>();
  public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

  public DbSet<Promotion> Promotions => Set<Promotion>();
  public DbSet<PromotionTarget> PromotionTargets => Set<PromotionTarget>();
  public DbSet<Voucher> Vouchers => Set<Voucher>();
  public DbSet<VoucherUsage> VoucherUsages => Set<VoucherUsage>();

  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderItem> OrderItems => Set<OrderItem>();
  public DbSet<OrderDiscount> OrderDiscounts => Set<OrderDiscount>();
  public DbSet<Payment> Payments => Set<Payment>();
  public DbSet<Invoice> Invoices => Set<Invoice>();

  public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
  public DbSet<Translation> Translations => Set<Translation>();
  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
