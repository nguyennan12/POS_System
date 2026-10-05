using System;

namespace POS.WinUI.Core.Constants;

///  
/// Tập trung tất cả URL endpoint của POS API.
/// Khi base path thay đổi chỉ cần sửa tại đây.
/// </summary>
public static class ApiRoutes
{
  public const string Health = "health";

  public static class Auth
  {
    public const string Login = "api/v1/auth/login";
    public const string PinLogin = "api/v1/auth/pin";
    public const string Refresh = "api/v1/auth/refresh";
    public const string Logout = "api/v1/auth/logout";
  }

  public static class Stores
  {
    public const string Public = "api/v1/stores/public";
    public const string All = "api/v1/stores";
  }

  public static class Shifts
  {
    public const string Open = "api/v1/shifts/open";
    public const string Current = "api/v1/shifts/current";
    public static string GetById(Guid id) => $"api/v1/shifts/{id}";
    public static string Close(Guid id) => $"api/v1/shifts/{id}/close";
  }

  public static class Categories
  {
    public const string Tree = "api/v1/categories";
    public static string GetById(Guid id) => $"api/v1/categories/{id}";
  }

  public static class Inventory
  {
    public const string Stock = "api/v1/inventory/stock";
    public const string Alerts = "api/v1/inventory/alerts";
  }

  public static class Customers
  {
    public const string Base = "api/v1/customers";
    public static string GetById(Guid id) => $"api/v1/customers/{id}";
    public static string Loyalty(Guid id) => $"api/v1/customers/{id}/loyalty";
  }

  public static class Vouchers
  {
    public const string Base = "api/v1/vouchers";
    public static string Validate(string code) => $"api/v1/vouchers/{Uri.EscapeDataString(code)}/validate";
  }

  public static class Products
  {
    public const string Base = "api/v1/products";
    public const string PosCatalog = "api/v1/products/pos-catalog";
    public static string GetById(Guid id) => $"api/v1/products/{id}";
    public const string BulkImport = "api/v1/products/bulk-import";
  }

  public static class Skus
  {
    public const string Base = "api/v1/skus";
    public static string GetById(Guid id) => $"api/v1/skus/{id}";
    public static string GetByBarcode(string code) => $"api/v1/skus/barcode/{Uri.EscapeDataString(code)}";
  }

  public static class Orders
  {
    public const string Base = "api/v1/orders";
    public static string GetById(Guid id) => $"api/v1/orders/{id}";
    public static string Items(Guid id) => $"api/v1/orders/{id}/items";
    public static string Vouchers(Guid id) => $"api/v1/orders/{id}/vouchers";
    public static string Checkout(Guid id) => $"api/v1/orders/{id}/checkout";
    public static string Cancel(Guid id) => $"api/v1/orders/{id}/cancel";
  }
}
