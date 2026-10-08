using System.Runtime.CompilerServices;
using FluentAssertions;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Categories.Commands.CreateCategory;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;

namespace POS.Application.Tests.Common.Behaviors;

public sealed class CommandPermissionMappingTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedPermissions =
        new Dictionary<string, string>
        {
            ["CreateCategoryCommand"] = "categories:manage",
            ["UpdateCategoryCommand"] = "categories:manage",
            ["DeleteCategoryCommand"] = "categories:manage",
            ["CreateCustomerCommand"] = "customers:create",
            ["UpdateCustomerCommand"] = "customers:manage",
            ["DeleteCustomerCommand"] = "customers:manage",
            ["UpdateMemberTierCommand"] = "member_tiers:manage",
            ["CreateEmployeeCommand"] = "employees:manage",
            ["UpdateEmployeeCommand"] = "employees:manage",
            ["LockEmployeeCommand"] = "employees:manage",
            ["ResetPasswordCommand"] = "employees:manage",
            ["ResetPinCommand"] = "employees:manage",
            ["CreateStockInVoucherCommand"] = "stock_in_vouchers:manage",
            ["CompleteStockInVoucherCommand"] = "stock_in_vouchers:complete",
            ["CancelStockInVoucherCommand"] = "stock_in_vouchers:manage",
            ["DisposeStockCommand"] = "inventory:dispose",
            ["CreateOrderCommand"] = "orders:create_own",
            ["AddOrderItemCommand"] = "orders:create_own",
            ["ApplyVoucherCommand"] = "vouchers:apply_own",
            ["CheckoutOrderCommand"] = "payments:create_own",
            ["CancelOrderCommand"] = "orders:cancel_own",
            ["CreateRoleCommand"] = "roles:manage",
            ["UpdateRoleCommand"] = "roles:manage",
            ["UpdateRolePermissionsCommand"] = "roles:manage",
            ["OpenShiftCommand"] = "shifts:manage_own",
            ["CloseShiftCommand"] = "shifts:manage_own",
            ["CreateStoreCommand"] = "stores:manage",
            ["CreateRegisterCommand"] = "stores:manage",
            ["UpdateStoreCommand"] = "stores:manage",
            ["UpdateStoreStatusCommand"] = "stores:manage",
            ["AssignAdminToStoreCommand"] = "stores:manage",
            ["CreateSupplierCommand"] = "suppliers:manage",
            ["UpdateSupplierCommand"] = "suppliers:manage",
            ["DeleteSupplierCommand"] = "suppliers:manage",
            ["CreateSupplierPaymentCommand"] = "suppliers:pay",
            ["CreatePromotionCommand"] = "promotions:manage",
            ["UpdatePromotionCommand"] = "promotions:manage",
            ["DeletePromotionCommand"] = "promotions:manage",
            ["CreateVoucherCommand"] = "vouchers:manage",
            ["UpdateVoucherCommand"] = "vouchers:manage",
            ["DeleteVoucherCommand"] = "vouchers:manage",
            ["AccruePointsCommand"] = "customers:manage",
            ["RedeemPointsCommand"] = "customers:manage",
            ["AdjustPointsCommand"] = "customers:loyalty_adjust",
            ["CreateProductCommand"] = "products:manage",
            ["UpdateProductCommand"] = "products:manage",
            ["CreateSkuCommand"] = "skus:manage",
            ["UpdateSkuCommand"] = "skus:manage",
            ["DeleteSkuCommand"] = "skus:manage",
            ["CreateUnitConversionCommand"] = "skus:manage",
            ["UpdateUnitConversionCommand"] = "skus:manage",
            ["CreatePriceListCommand"] = "skus:price_override",
            ["BulkImportProductsCommand"] = "products:import",
        };

    private static readonly HashSet<string> AuthenticationCommands =
    [
        "EmployeeLoginWithPasswordCommand",
        "EmployeeLoginWithPinCommand",
        "RefreshTokenCommand",
        "LogoutCommand",
        "ChangePasswordCommand",
        "ChangePinCommand",
    ];

    // Invoked by checkout after its authorization, inside its existing transaction.
    private static readonly HashSet<string> InternalCommands = [nameof(GenerateInvoiceCommand)];

    [Fact]
    public void Commands_ShouldDeclareExpectedBusinessPermissions()
    {
        Type[] commandTypes = typeof(CreateCategoryCommand).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && IsCommand(type))
            .ToArray();

        commandTypes
            .GroupBy(type => type.Name)
            .Where(group => group.Count() > 1)
            .Should()
            .BeEmpty("command names are used as stable keys in this exhaustive permission test");

        commandTypes
            .Select(type => type.Name)
            .Should()
            .BeEquivalentTo(ExpectedPermissions.Keys.Concat(AuthenticationCommands).Concat(InternalCommands));

        foreach (Type commandType in commandTypes.Where(type => ExpectedPermissions.ContainsKey(type.Name)))
        {
            commandType.Should().Implement<IRequirePermission>();

            var command = (IRequirePermission)RuntimeHelpers.GetUninitializedObject(commandType);
            command.RequiredPermission.Should().Be(ExpectedPermissions[commandType.Name]);
        }

        foreach (Type commandType in commandTypes.Where(type => AuthenticationCommands.Contains(type.Name)))
        {
            commandType.Should().NotImplement<IRequirePermission>(
                "authentication lifecycle commands must remain callable before business authorization");
        }

        foreach (Type commandType in commandTypes.Where(type => InternalCommands.Contains(type.Name)))
        {
            commandType.Should().NotImplement<IRequirePermission>(
                "internal commands inherit the caller's authorized workflow");
        }
    }

    private static bool IsCommand(Type type) =>
        type.GetInterfaces().Any(@interface =>
            @interface == typeof(ICommand) ||
            (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICommand<>)));
}
