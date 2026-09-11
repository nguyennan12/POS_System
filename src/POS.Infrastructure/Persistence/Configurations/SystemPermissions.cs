using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public record ResourceDefinition(
    string Code,
    string Description,
    (PermissionAction Action, string Description)[] Actions
);

public static class SystemPermissions
{
    public static readonly ResourceDefinition[] Resources =
    [
        // 1. Store Management
        new(ResourceNames.Stores, "Store Management",
        [
            (PermissionAction.Create, "Create new store"),
            (PermissionAction.Read, "View store details and list"),
            (PermissionAction.Update, "Update store information and settings"),
            (PermissionAction.Delete, "Delete store")
        ]),

        // 2. Employee Management
        new(ResourceNames.Employees, "Employee Management",
        [
            (PermissionAction.Create, "Create new employee account"),
            (PermissionAction.Read, "View employee profiles and list"),
            (PermissionAction.Update, "Update employee information and status"),
            (PermissionAction.Delete, "Delete or deactivate employee account")
        ]),

        // 3. Role & Permission Management
        new(ResourceNames.Roles, "Role and Permission Management",
        [
            (PermissionAction.Create, "Create new custom role"),
            (PermissionAction.Read, "View roles and assigned permission matrix"),
            (PermissionAction.Update, "Update role details and permissions"),
            (PermissionAction.Delete, "Delete custom role")
        ]),

        // 4. Product Management
        new(ResourceNames.Products, "Product Catalog Management",
        [
            (PermissionAction.Create, "Create new product"),
            (PermissionAction.Read, "View product catalog, pricing, and details"),
            (PermissionAction.Update, "Update product information and pricing"),
            (PermissionAction.Delete, "Delete product")
        ]),

        // 5. Category Management
        new(ResourceNames.Categories, "Category Management",
        [
            (PermissionAction.Create, "Create new product category"),
            (PermissionAction.Read, "View product category list"),
            (PermissionAction.Update, "Update product category details"),
            (PermissionAction.Delete, "Delete product category")
        ]),

        // 6. Inventory Management
        new(ResourceNames.Inventory, "Inventory and Stock Management",
        [
            (PermissionAction.Create, "Create stock entry or inventory slip"),
            (PermissionAction.Read, "View stock levels and inventory movement"),
            (PermissionAction.Update, "Update inventory quantities and slips"),
            (PermissionAction.Delete, "Delete or void inventory slip")
        ]),

        // 7. Order & Sales Management
        new(ResourceNames.Orders, "Order and Sales Management",
        [
            (PermissionAction.Create, "Create new sales order"),
            (PermissionAction.Read, "View order details and sales history"),
            (PermissionAction.Update, "Update order status and details"),
            (PermissionAction.Delete, "Cancel or delete sales order")
        ]),

        // 8. Customer Management
        new(ResourceNames.Customers, "Customer Management",
        [
            (PermissionAction.Create, "Create new customer profile"),
            (PermissionAction.Read, "View customer profiles and purchase history"),
            (PermissionAction.Update, "Update customer details and reward points"),
            (PermissionAction.Delete, "Delete customer profile")
        ]),

        // 9. Discount & Promotion Management
        new(ResourceNames.Discounts, "Discount and Promotion Management",
        [
            (PermissionAction.Create, "Create new promotional campaign or discount"),
            (PermissionAction.Read, "View discount list and active promotions"),
            (PermissionAction.Update, "Update discount rules and validity"),
            (PermissionAction.Delete, "Delete promotional campaign or discount")
        ]),

        // 10. Reports & Analytics Management
        new(ResourceNames.Reports, "Reports and Analytics Management",
        [
            (PermissionAction.Create, "Generate new analytics report"),
            (PermissionAction.Read, "View sales, revenue, and inventory reports"),
            (PermissionAction.Update, "Update report parameters and settings"),
            (PermissionAction.Delete, "Delete saved report")
        ])
    ];
}
