using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Queries.GetOrders;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Orders;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders.Enums;

namespace POS.Application.Tests.Orders;

public class OrderReadQueryTests
{
    private readonly IOrderRepository orders = Substitute.For<IOrderRepository>();
    private readonly IEmployeeRepository employees = Substitute.For<IEmployeeRepository>();
    private readonly ICurrentUser user = Substitute.For<ICurrentUser>();
    private readonly Employee employee = new("Cashier", "cashier", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());

    public OrderReadQueryTests()
    {
        user.EmployeeId.Returns(employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
    }

    private GetOrdersQueryHandler ListHandler() => new(orders, employees, user);

    [Theory]
    [InlineData(1, 20, 25, 20)]
    [InlineData(2, 20, 25, 5)]
    [InlineData(1, 100, 101, 100)]
    [InlineData(1, 20, 0, 0)]
    public async Task List_MapsPagingAndForwardsFiltersWithinPersistedEmployeeScope(int page, int size, int total, int count)
    {
        var filter = new OrderFilterRequest(
            StoreId: employee.StoreId,
            ShiftId: Guid.NewGuid(),
            Status: "Paid",
            From: DateTimeOffset.UtcNow.AddDays(-1),
            To: DateTimeOffset.UtcNow,
            PageNumber: page,
            PageSize: size);

        var items = Enumerable.Range(0, count).Select(i => new OrderSummaryDto(
            Guid.NewGuid(),
            employee.StoreId!.Value,
            filter.ShiftId!.Value,
            null,
            null,
            null,
            "Paid",
            "VND",
            100000m,
            2,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow)).ToList();

        using var cancellation = new CancellationTokenSource();
        orders.GetPagedAsync(employee.StoreId, filter.ShiftId, OrderStatus.Paid, filter.From, filter.To,
            page, size, cancellation.Token).Returns((items, total));

        var result = await ListHandler().Handle(new(filter), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(new PagedResponse<OrderSummaryDto>(items, page, size, total), result.Value);
        Assert.Equal(page > 1, result.Value!.HasPreviousPage);
        Assert.Equal(page * size < total, result.Value.HasNextPage);
        await orders.Received(1).GetPagedAsync(employee.StoreId, filter.ShiftId, OrderStatus.Paid,
            filter.From, filter.To, page, size, cancellation.Token);
    }

    [Fact]
    public async Task List_EnforcesEmployeeStoreWhenNotChainOwner()
    {
        var otherStoreId = Guid.NewGuid();
        var filter = new OrderFilterRequest(StoreId: otherStoreId);

        var result = await ListHandler().Handle(new(filter), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.InvalidStore, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Empty(orders.ReceivedCalls());
    }

    [Fact]
    public async Task List_AllowsChainOwnerToQueryAnyStoreOrAllStores()
    {
        var owner = new Employee("Owner", "owner", "hash", "hash", Guid.NewGuid(), isChainOwner: true, id: employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(owner);

        var otherStoreId = Guid.NewGuid();
        var filter = new OrderFilterRequest(StoreId: otherStoreId);

        orders.GetPagedAsync(otherStoreId, null, null, null, null, 1, 20, default)
            .Returns((new List<OrderSummaryDto>(), 0));

        var result = await ListHandler().Handle(new(filter), default);

        Assert.True(result.IsSuccess);
        await orders.Received(1).GetPagedAsync(otherStoreId, null, null, null, null, 1, 20, default);
    }

    [Fact]
    public async Task List_UsesDefaultPagingWhenEmpty()
    {
        orders.GetPagedAsync(employee.StoreId, null, null, null, null, 1, 20, default)
            .Returns((new List<OrderSummaryDto>(), 0));

        var result = await ListHandler().Handle(new(new()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public void ListValidator_RejectsInvalidPaging(int page, int size)
    {
        var validator = new GetOrdersQueryValidator();
        var result = validator.Validate(new GetOrdersQuery(new(PageNumber: page, PageSize: size)));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ListValidator_AcceptsBoundariesAndRejectsReversedRangeOrEmptyGuids()
    {
        var validator = new GetOrdersQueryValidator();
        var instant = DateTimeOffset.UtcNow;

        Assert.True(validator.Validate(new GetOrdersQuery(new())).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(PageSize: 1))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(PageSize: 100))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(From: instant, To: instant.ToOffset(TimeSpan.FromHours(7))))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(From: instant))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(To: instant))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(Status: "Paid"))).IsValid);
        Assert.True(validator.Validate(new GetOrdersQuery(new(Status: "draft"))).IsValid);

        Assert.False(validator.Validate(new GetOrdersQuery(new(From: instant, To: instant.AddTicks(-1)))).IsValid);
        Assert.False(validator.Validate(new GetOrdersQuery(new(StoreId: Guid.Empty))).IsValid);
        Assert.False(validator.Validate(new GetOrdersQuery(new(ShiftId: Guid.Empty))).IsValid);
        Assert.False(validator.Validate(new GetOrdersQuery(new(Status: "UnknownStatus"))).IsValid);
        Assert.False(validator.Validate(new GetOrdersQuery(null!)).IsValid);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("missing")]
    [InlineData("inactive")]
    public async Task List_RejectsInvalidEmployee(string state)
    {
        if (state == "anonymous") user.EmployeeId.Returns((Guid?)null);
        if (state == "missing") employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns((Employee?)null);
        if (state == "inactive") employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(
            new Employee("Inactive", "inactive", "hash", "hash", Guid.NewGuid(), isActive: false, id: employee.Id));

        var result = await ListHandler().Handle(new(new()), default);

        Assert.Equal(OrderErrors.Unauthorized, result.Error);
        Assert.Empty(orders.ReceivedCalls());
    }

    [Theory]
    [InlineData(false, false, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    [InlineData(true, true, ErrorType.None)]
    public async Task List_RequiresOrdersReadPermission(bool authenticated, bool allowed, ErrorType expected)
    {
        user.IsAuthenticated.Returns(authenticated);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>())
            .Returns(allowed ? ["orders:read"] : ["orders:update"]);

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(employees);
        services.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());

        var query = new GetOrdersQuery(new());
        Assert.Equal("orders:read", query.RequiredPermission);

        var called = false;
        var result = await new AuthorizationBehavior<GetOrdersQuery, Result<PagedResponse<OrderSummaryDto>>>(user, services).Handle(query, () =>
        {
            called = true;
            return Task.FromResult(Result<PagedResponse<OrderSummaryDto>>.Success(new PagedResponse<OrderSummaryDto>([], 1, 20, 0)));
        }, default);

        Assert.Equal(expected == ErrorType.None, called);
        Assert.Equal(expected, result.Error.Type);
    }
}
