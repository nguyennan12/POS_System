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
            ["CreateCategoryCommand"] = "categories:create",
            ["UpdateCategoryCommand"] = "categories:update",
            ["DeleteCategoryCommand"] = "categories:delete",
            ["CreateCustomerCommand"] = "customers:create",
            ["UpdateCustomerCommand"] = "customers:update",
            ["DeleteCustomerCommand"] = "customers:delete",
            ["UpdateMemberTierCommand"] = "customers:update",
            ["CreateEmployeeCommand"] = "employees:create",
            ["UpdateEmployeeCommand"] = "employees:update",
            ["LockEmployeeCommand"] = "employees:update",
            ["ResetPasswordCommand"] = "employees:update",
            ["ResetPinCommand"] = "employees:update",
            ["CreateStockInVoucherCommand"] = "inventory:create",
            ["CompleteStockInVoucherCommand"] = "inventory:update",
            ["CancelStockInVoucherCommand"] = "inventory:delete",
            ["DisposeStockCommand"] = "inventory:update",
            ["CreateOrderCommand"] = "orders:create",
            ["AddOrderItemCommand"] = "orders:update",
            ["ApplyVoucherCommand"] = "orders:update",
            ["CheckoutOrderCommand"] = "orders:update",
            ["CancelOrderCommand"] = "orders:update",
            ["CreateRoleCommand"] = "roles:create",
            ["UpdateRoleCommand"] = "roles:update",
            ["UpdateRolePermissionsCommand"] = "roles:update",
            ["OpenShiftCommand"] = "shifts:create",
            ["CloseShiftCommand"] = "shifts:update",
            ["CreateStoreCommand"] = "stores:create",
            ["UpdateStoreCommand"] = "stores:update",
            ["UpdateStoreStatusCommand"] = "stores:update",
            ["AssignAdminToStoreCommand"] = "stores:update",
            ["CreateSupplierCommand"] = "suppliers:create",
            ["UpdateSupplierCommand"] = "suppliers:update",
            ["DeleteSupplierCommand"] = "suppliers:delete",
            ["CreateSupplierPaymentCommand"] = "suppliers:update",
            ["CreatePromotionCommand"] = "discounts:create",
            ["UpdatePromotionCommand"] = "discounts:update",
            ["DeletePromotionCommand"] = "discounts:delete",
            ["CreateVoucherCommand"] = "discounts:create",
            ["UpdateVoucherCommand"] = "discounts:update",
            ["DeleteVoucherCommand"] = "discounts:delete",
            ["AccruePointsCommand"] = "customers:update",
            ["RedeemPointsCommand"] = "customers:update",
            ["AdjustPointsCommand"] = "customers:update",
            ["CreateProductCommand"] = "products:create",
            ["UpdateProductCommand"] = "products:update",
            ["CreateSkuCommand"] = "products:create",
            ["UpdateSkuCommand"] = "products:update",
            ["DeleteSkuCommand"] = "products:delete",
            ["CreateUnitConversionCommand"] = "products:create",
            ["UpdateUnitConversionCommand"] = "products:update",
            ["CreatePriceListCommand"] = "products:create",
            ["BulkImportProductsCommand"] = "products:create",
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
