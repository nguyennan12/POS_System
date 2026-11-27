using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.AI.Tools;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.AI.Tools.Invoices;
using POS.Application.UseCases.AI.Tools.Orders;
using POS.Application.UseCases.AI.Tools.Reports;
using POS.Application.UseCases.Invoices.Queries.GetInvoiceById;
using POS.Application.UseCases.Invoices.Queries.GetInvoices;
using POS.Application.UseCases.Orders.Access;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrderById;
using POS.Application.UseCases.Orders.Queries.GetAccessibleOrders;
using POS.Application.UseCases.Reports.Queries.GetRevenueReport;
using POS.Application.UseCases.Reports.Queries.GetTopSellingReport;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Tests.AI;

public sealed class AiOrdersInvoicesReportsToolsTests
{
    [Fact]
    public async Task CashierWithReadOwn_ListsOnlyOwnOrders_AndCannotReadOtherCashiersOrderByGuid()
    {
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var cashierA = Guid.NewGuid();
        var cashierB = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(cashierA);
        currentUser.StoreId.Returns(storeId);

        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeId));

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{cashierA}", Arg.Any<CancellationToken>())
            .Returns(["orders:read_own"]); // Cashier chỉ có orders:read_own, KHÔNG có orders:read

        var employeeRepo = Substitute.For<IEmployeeRepository>();
        var permissionRepo = Substitute.For<IPermissionRepository>();
        var orderRepo = Substitute.For<IOrderRepository>();

        var policy = new OrderReadAccessPolicy(currentUser, storePolicy, cache, employeeRepo, permissionRepo);
        var listQueryHandler = new GetAccessibleOrdersQueryHandler(policy, orderRepo);
        var detailQueryHandler = new GetAccessibleOrderByIdQueryHandler(policy, orderRepo);

        orderRepo.GetPagedByScopeAsync(
                storeId,
                cashierA,
                OrderStatus.Paid,
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                1,
                10,
                Arg.Any<CancellationToken>())
            .Returns((
                new List<OrderSummaryDto>
                {
                    new(Guid.NewGuid(), storeId, shiftId, null, null, null, "Paid", "VND", 55000m, 2, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
                },
                1));

        var listResult = await listQueryHandler.Handle(
            new GetAccessibleOrdersQuery(OrderStatus.Paid, null, null, 10),
            CancellationToken.None);

        listResult.IsSuccess.Should().BeTrue();
        listResult.Value.Items.Should().HaveCount(1);
        await orderRepo.Received(1).GetPagedByScopeAsync(
            storeId,
            cashierA, // Bắt buộc filter CreatedBy == cashierA ở tầng repository
            OrderStatus.Paid,
            null,
            null,
            1,
            10,
            Arg.Any<CancellationToken>());

        // Đơn của Cashier B trong cùng cửa hàng -> Cashier A biết GUID vẫn bị chặn PERMISSION_DENIED
        var otherOrder = Order.CreateDraft(storeId, shiftId, cashierB);
        orderRepo.GetByIdWithDetailsAsync(otherOrder.Id, Arg.Any<CancellationToken>()).Returns(otherOrder);

        var otherDetailResult = await detailQueryHandler.Handle(
            new GetAccessibleOrderByIdQuery(otherOrder.Id),
            CancellationToken.None);

        otherDetailResult.IsFailure.Should().BeTrue();
        AiToolErrors.FromDomainError(otherDetailResult.Error).ErrorCode.Should().Be(AiToolErrors.PermissionDenied);

        // Đơn của chính Cashier A -> đọc thành công
        var ownOrder = Order.CreateDraft(storeId, shiftId, cashierA);
        orderRepo.GetByIdWithDetailsAsync(ownOrder.Id, Arg.Any<CancellationToken>()).Returns(ownOrder);

        var ownDetailResult = await detailQueryHandler.Handle(
            new GetAccessibleOrderByIdQuery(ownOrder.Id),
            CancellationToken.None);

        ownDetailResult.IsSuccess.Should().BeTrue();
        ownDetailResult.Value.Id.Should().Be(ownOrder.Id);
    }

    [Fact]
    public async Task ManagerWithOrdersRead_ListsAllOrdersInCurrentStore()
    {
        var storeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(managerId);

        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeId));

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{managerId}", Arg.Any<CancellationToken>())
            .Returns(["orders:read"]);

        var orderRepo = Substitute.For<IOrderRepository>();
        orderRepo.GetPagedByScopeAsync(
                storeId,
                null,
                null,
                null,
                null,
                1,
                20,
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderSummaryDto>(), 0));

        var policy = new OrderReadAccessPolicy(
            currentUser,
            storePolicy,
            cache,
            Substitute.For<IEmployeeRepository>(),
            Substitute.For<IPermissionRepository>());
        var handler = new GetAccessibleOrdersQueryHandler(policy, orderRepo);

        var res = await handler.Handle(new GetAccessibleOrdersQuery(null, null, null, 20), CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        await orderRepo.Received(1).GetPagedByScopeAsync(
            storeId,
            null, // Manager xem toàn bộ đơn của store hiện tại
            null,
            null,
            null,
            1,
            20,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvoiceTools_ScopeStore_AndProjectExactDtoFields()
    {
        var storeId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var sender = Substitute.For<ISender>();
        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeId));

        sender.Send(
                Arg.Is<GetInvoicesQuery>(q => q.Filter.StoreId == storeId && q.Filter.Search == "HD001" && q.Filter.PaymentMethod == "Cash"),
                Arg.Any<CancellationToken>())
            .Returns(Result<PagedResponse<InvoiceSummaryResponse>>.Success(
                new PagedResponse<InvoiceSummaryResponse>(
                    [new InvoiceSummaryResponse(invoiceId, orderId, "HD001", "Nguyễn Văn A", 100000m, 8000m, 108000m, DateTimeOffset.UtcNow)],
                    1,
                    20,
                    1)));

        sender.Send(Arg.Is<GetInvoiceByIdQuery>(q => q.Id == invoiceId), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDetailResponse>.Success(
                new InvoiceDetailResponse(
                    invoiceId,
                    orderId,
                    "HD001",
                    "Nguyễn Văn A",
                    "0101234567",
                    "TP.HCM",
                    100000m,
                    8000m,
                    108000m,
                    DateTimeOffset.UtcNow,
                    [new InvoiceItemResponse(Guid.NewGuid(), "Trà đào", "TD-01", 2m, 50000m, 100000m)])));

        var searchTool = new SearchInvoicesToolHandler(sender, storePolicy);
        using var searchArgs = JsonDocument.Parse("""{"search":"HD001","paymentMethod":"cash","limit":10}""");
        var searchResult = await searchTool.ExecuteAsync(searchArgs.RootElement, CancellationToken.None);
        searchResult.Success.Should().BeTrue();
        searchResult.ResultCount.Should().Be(1);

        var detailTool = new GetInvoiceDetailToolHandler(sender, storePolicy);
        using var detailArgs = JsonDocument.Parse($$"""{"invoiceId":"{{invoiceId}}"}""");
        var detailResult = await detailTool.ExecuteAsync(detailArgs.RootElement, CancellationToken.None);
        detailResult.Success.Should().BeTrue();
        detailResult.ResultCount.Should().Be(1);
    }

    [Fact]
    public async Task ReportTools_RejectInvalidDateRangeWithoutCallingSender_AndMapForbiddenSafely()
    {
        var storeId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeId));

        var revenueTool = new GetRevenueReportToolHandler(sender, storePolicy);
        var topSellingTool = new GetTopSellingProductsToolHandler(sender, storePolicy);

        // 1. from > to -> trả INVALID_ARGUMENTS ngay lập tức, không gọi ISender
        using var invalidRangeArgs = JsonDocument.Parse("""{"from":"2026-11-25","to":"2026-11-01"}""");
        var badRangeResult = await revenueTool.ExecuteAsync(invalidRangeArgs.RootElement, CancellationToken.None);
        badRangeResult.Success.Should().BeFalse();
        badRangeResult.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);
        await sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<GetRevenueReportQuery>(), Arg.Any<CancellationToken>());

        // 2. Thiếu quyền reports:revenue_view -> trả PERMISSION_DENIED an toàn
        sender.Send(Arg.Any<GetRevenueReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<RevenueReportResponse>.Failure(
                new Error(ErrorType.Forbidden, "Authorization.Forbidden", "Không có quyền")));

        using var validRangeArgs = JsonDocument.Parse("""{"from":"2026-11-01","to":"2026-11-25"}""");
        var deniedResult = await revenueTool.ExecuteAsync(validRangeArgs.RootElement, CancellationToken.None);
        deniedResult.Success.Should().BeFalse();
        deniedResult.ErrorCode.Should().Be(AiToolErrors.PermissionDenied);

        // 3. Top selling giới hạn tối đa 20 và truyền đúng StoreId
        sender.Send(
                Arg.Is<GetTopSellingReportQuery>(q => q.StoreId == storeId && q.Limit == 5),
                Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TopSellingProductItem>>.Success(
                [new TopSellingProductItem(Guid.NewGuid(), "CF-01", "Cà phê đen", 85m, 2125000m)]));

        using var topArgs = JsonDocument.Parse("""{"from":"2026-11-01","to":"2026-11-25","limit":5}""");
        var topResult = await topSellingTool.ExecuteAsync(topArgs.RootElement, CancellationToken.None);
        topResult.Success.Should().BeTrue();
        topResult.ResultCount.Should().Be(1);
    }

    [Fact]
    public void ServiceCollection_RegistersAll10Person1Handlers_AndAuditedExecutor()
    {
        var services = new ServiceCollection();
        services.AddAiTooling();

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IAiToolRegistry) &&
            d.ImplementationType == typeof(AiToolRegistry) &&
            d.Lifetime == ServiceLifetime.Singleton);

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IAiToolExecutor) &&
            d.ImplementationType == typeof(AuditedAiToolExecutor) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IOrderReadAccessPolicy) &&
            d.ImplementationType == typeof(OrderReadAccessPolicy) &&
            d.Lifetime == ServiceLifetime.Scoped);

        var registeredHandlerTypes = services
            .Where(d => d.ServiceType == typeof(IAiToolHandler))
            .Select(d => d.ImplementationType)
            .ToArray();

        registeredHandlerTypes.Should().HaveCount(10);
        registeredHandlerTypes.Should().OnlyHaveUniqueItems();

        var sender = Substitute.For<ISender>();
        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        var instantiatedNames = new IAiToolHandler[]
        {
            new POS.Application.UseCases.AI.Tools.Products.SearchProductsToolHandler(sender),
            new POS.Application.UseCases.AI.Tools.Products.GetProductDetailToolHandler(sender),
            new POS.Application.UseCases.AI.Tools.Inventory.GetInventoryToolHandler(sender, storePolicy),
            new POS.Application.UseCases.AI.Tools.Inventory.GetStockAlertsToolHandler(sender, storePolicy),
            new ListOrdersToolHandler(sender),
            new GetOrderDetailToolHandler(sender),
            new SearchInvoicesToolHandler(sender, storePolicy),
            new GetInvoiceDetailToolHandler(sender, storePolicy),
            new GetRevenueReportToolHandler(sender, storePolicy),
            new GetTopSellingProductsToolHandler(sender, storePolicy)
        }.Select(h => h.Name).ToArray();

        instantiatedNames.Should().Equal(
            new AiToolRegistry().GetAvailableTools().Take(10).Select(t => t.Name));
    }
}
