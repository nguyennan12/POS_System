using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Contracts.V1.Reports;
using POS.Domain.Customers;
using POS.Domain.Inventory.Stock;

namespace POS.Application.Tests.AI;

public sealed class OperationalAiToolTests
{
    [Fact]
    public void OperationalHandlersAreRegisteredUnderTheirSchemaNames()
    {
        var host = new AiToolTestHost();
        using var provider = host.CreateServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var schemas = provider.GetRequiredService<IAiToolRegistry>().GetAvailableTools();
        var names = scope.ServiceProvider.GetServices<IAiToolHandler>().Select(handler => handler.Name).ToArray();

        foreach (var index in new[] { 10, 11, 12, 13, 15, 16, 17, 18 })
            Assert.Single(names, name => name == schemas[index].Name);
    }

    [Theory]
    [InlineData(AiToolNames.SearchCustomers, """{"phone":" ","limit":5}""")]
    [InlineData(AiToolNames.GetCurrentShift, """{"storeId":"invalid"}""")]
    [InlineData(AiToolNames.GetDashboardSummary, """{"days":91}""")]
    [InlineData(AiToolNames.GetSlowMovingProducts, """{"days":0}""")]
    [InlineData(AiToolNames.GetExpiringBatches, """{"expiryBefore":"invalid"}""")]
    [InlineData(AiToolNames.GetExpiringBatches, """{"expiryBefore":"12/31/2026"}""")]
    public async Task RejectsInvalidArguments(string name, string arguments)
    {
        using var provider = new AiToolTestHost().CreateServices().BuildServiceProvider();
        var result = await AiToolTestHost.ExecuteAsync(provider, name, arguments);
        Assert.Equal(AiToolErrors.InvalidArguments, result.ErrorCode);
    }

    [Fact]
    public async Task CustomerSearchScopesTheQueryAndProjectsOnlySalesFields()
    {
        var host = new AiToolTestHost("customers:read");
        var repository = Substitute.For<ICustomerRepository>();
        var customer = new Customer("Khách hàng", "0901234567", Guid.NewGuid(),
            email: "customer@example.test", dob: new DateOnly(2000, 1, 1), barcode: "member-code");
        repository.GetPagedAsync("090", null, null, null, 1, 10, Arg.Any<CancellationToken>(), host.StoreId)
            .Returns((new List<CustomerWithPoints> { new(customer, 15m) }, 1));
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.SearchCustomers, """{"phone":" 090 "}""");

        Assert.True(result.Success);
        var item = Assert.Single(JsonSerializer.SerializeToElement(result.Data).EnumerateArray());
        Assert.Equal(new[] { "CustomerId", "FullName", "Phone", "MemberTierName", "PointsBalance" },
            item.EnumerateObject().Select(property => property.Name));
        await repository.Received(1)
            .GetPagedAsync("090", null, null, null, 1, 10, Arg.Any<CancellationToken>(), host.StoreId);
    }

    [Theory]
    [InlineData(AiToolNames.GetCustomerDetail)]
    [InlineData(AiToolNames.GetCustomerLoyalty)]
    public async Task CustomerOutsideBranchScopeIsNotLoaded(string name)
    {
        var host = new AiToolTestHost("customers:read");
        var repository = Substitute.For<ICustomerRepository>();
        var customerId = Guid.NewGuid();
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, name, $$"""{"customerId":"{{customerId}}"}""");

        Assert.Equal(AiToolErrors.NotFound, result.ErrorCode);
        await repository.Received(1).HasOrdersAtStoreAsync(customerId, host.StoreId, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetByIdAsync(customerId, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetEntityByIdAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CurrentShiftUsesTheEmployeeStoreAndReturnsAnExplicitClosedState()
    {
        var host = new AiToolTestHost("shifts:manage_own");
        var repository = Substitute.For<IShiftRepository>();
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetCurrentShift, "{}");

        Assert.True(result.Success);
        Assert.False(JsonSerializer.SerializeToElement(result.Data).GetProperty("IsOpen").GetBoolean());
        await repository.Received(1).GetOpenShiftAsync(host.StoreId, null, Arg.Any<CancellationToken>(), host.EmployeeId);
    }

    [Fact]
    public async Task DashboardAcceptsTheFullToolRangeThroughMediatR()
    {
        var host = new AiToolTestHost("reports:dashboard_view");
        var repository = Substitute.For<IReportRepository>();
        repository.GetDashboardSummaryAsync(host.StoreId, 90, Arg.Any<CancellationToken>())
            .Returns(new DashboardSummaryResponse(0, 0, 0, 0, [], [], new DashboardAlerts(0, 0, 0)));
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetDashboardSummary, """{"days":90}""");

        Assert.True(result.Success);
        await repository.Received(1).GetDashboardSummaryAsync(host.StoreId, 90, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SlowMovingProductsRequiresInventoryReportPermission()
    {
        var host = new AiToolTestHost("reports:dashboard_view");
        var repository = Substitute.For<IReportRepository>();
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetSlowMovingProducts, "{}");

        Assert.Equal(AiToolErrors.PermissionDenied, result.ErrorCode);
        await repository.DidNotReceive()
            .GetSlowMovingItemsAsync(Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockHistoryIsScopedAndOmitsUnitCost()
    {
        var host = new AiToolTestHost("inventory:read");
        var repository = Substitute.For<IStockTransactionRepository>();
        var sku = Guid.NewGuid();
        var transaction = StockTransaction.CreateInitialStock(host.StoreId, sku, 1, host.EmployeeId, 12000);
        repository.GetPagedAsync(host.StoreId, sku, "StockIn", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<StockTransaction> { transaction }, 1));
        using var provider = host.CreateServices().AddSingleton(repository).BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetStockHistory,
            $$"""{"skuId":"{{sku}}","type":"stockin"}""");

        Assert.True(result.Success);
        var item = Assert.Single(JsonSerializer.SerializeToElement(result.Data).EnumerateArray());
        Assert.False(item.TryGetProperty("UnitCost", out _));
        Assert.Equal("StockIn", item.GetProperty("Type").GetString());
    }

    [Fact]
    public async Task ExpiringBatchesUsesSelectedOwnerStoreAndInclusiveExpiryCutoff()
    {
        var host = new AiToolTestHost("inventory:read");
        var repository = Substitute.For<IStockEntryRepository>();
        var cutoff = new DateOnly(2026, 12, 31);
        repository.GetBatchesAsync(host.StoreId, null, cutoff, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<StockBatch>(), 0));
        var services = host.CreateServices().AddSingleton(repository);
        host.CurrentUser.IsChainOwner.Returns(true);
        using var provider = services.BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetExpiringBatches,
            """{"expiryBefore":"2026-12-31"}""");

        Assert.True(result.Success);
        Assert.Empty(JsonSerializer.SerializeToElement(result.Data).EnumerateArray());
        await repository.Received(1).GetBatchesAsync(host.StoreId, null, cutoff, 1, 20, Arg.Any<CancellationToken>());
    }
}
