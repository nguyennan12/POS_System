using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Invoices.Queries.GetInvoiceById;
using POS.Application.UseCases.Invoices.Queries.GetInvoices;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Products;

namespace POS.Application.Tests.Invoices;

public class InvoiceReadQueryTests
{
    private readonly IInvoiceRepository invoices = Substitute.For<IInvoiceRepository>();
    private readonly IEmployeeRepository employees = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeStoreAccessRepository accesses = Substitute.For<IEmployeeStoreAccessRepository>();
    private readonly ICurrentUser user = Substitute.For<ICurrentUser>();
    private readonly Employee employee = new("Employee", "employee", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());

    public InvoiceReadQueryTests()
    {
        user.EmployeeId.Returns(employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
    }

    private GetInvoicesQueryHandler ListHandler() => new(invoices, employees, user);
    private GetInvoiceByIdQueryHandler DetailHandler() => new(invoices, employees, accesses, user);

    [Theory]
    [InlineData(1, 20, 25, 20)]
    [InlineData(2, 20, 25, 5)]
    [InlineData(1, 100, 101, 100)]
    [InlineData(1, 20, 0, 0)]
    public async Task List_MapsPagingAndForwardsFiltersWithinPersistedEmployeeScope(int page, int size, int total, int count)
    {
        var filter = new InvoiceFilterRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, page, size);
        var items = Enumerable.Range(0, count).Select(i => new InvoiceSummaryResponse(
            Guid.NewGuid(), filter.OrderId!.Value, $"HD-{i}", "Buyer", 100, 10, 110, DateTimeOffset.UtcNow)).ToList();
        using var cancellation = new CancellationTokenSource();
        invoices.GetPagedAsync(employee.Id, employee.StoreId, false, filter.OrderId, filter.From, filter.To,
            page, size, cancellation.Token).Returns((items, total));
        // Scope must come from the persisted employee, not stale token claims.
        user.IsChainOwner.Returns(true);
        user.StoreId.Returns(Guid.NewGuid());

        var result = await ListHandler().Handle(new(filter), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(new PagedResponse<InvoiceSummaryResponse>(items, page, size, total), result.Value);
        Assert.Equal(page > 1, result.Value!.HasPreviousPage);
        Assert.Equal(page * size < total, result.Value.HasNextPage);
        await invoices.Received(1).GetPagedAsync(employee.Id, employee.StoreId, false, filter.OrderId,
            filter.From, filter.To, page, size, cancellation.Token);
    }

    [Fact]
    public async Task List_UsesDefaultPaging()
    {
        invoices.GetPagedAsync(employee.Id, employee.StoreId, false, null, null, null, 1, 20, default)
            .Returns((new List<InvoiceSummaryResponse>(), 0));
        var result = await ListHandler().Handle(new(new()), default);
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
    public void ListValidator_RejectsInvalidPaging(int page, int size) =>
        Assert.False(new GetInvoicesQueryValidator().Validate(new GetInvoicesQuery(new(PageNumber: page, PageSize: size))).IsValid);

    [Fact]
    public void ListValidator_AcceptsBoundariesAndRejectsReversedRangeOrEmptyOrderId()
    {
        var validator = new GetInvoicesQueryValidator();
        var instant = DateTimeOffset.UtcNow;
        Assert.True(validator.Validate(new GetInvoicesQuery(new())).IsValid);
        Assert.True(validator.Validate(new GetInvoicesQuery(new(PageSize: 1))).IsValid);
        Assert.True(validator.Validate(new GetInvoicesQuery(new(PageSize: 100))).IsValid);
        Assert.True(validator.Validate(new GetInvoicesQuery(new(From: instant, To: instant.ToOffset(TimeSpan.FromHours(7))))).IsValid);
        Assert.True(validator.Validate(new GetInvoicesQuery(new(From: instant))).IsValid);
        Assert.True(validator.Validate(new GetInvoicesQuery(new(To: instant))).IsValid);
        Assert.False(validator.Validate(new GetInvoicesQuery(new(From: instant, To: instant.AddTicks(-1)))).IsValid);
        Assert.False(validator.Validate(new GetInvoicesQuery(new(OrderId: Guid.Empty))).IsValid);
        Assert.False(validator.Validate(new GetInvoicesQuery(null!)).IsValid);
    }

    [Fact]
    public async Task Detail_MapsInvoiceSnapshotAndOrderItems()
    {
        var invoice = SetUpInvoice(employee.StoreId!.Value);
        var result = await DetailHandler().Handle(new(invoice.Id), default);
        Assert.True(result.IsSuccess);
        var response = result.Value!;
        Assert.Equal(invoice.Id, response.Id);
        Assert.Equal(invoice.OrderId, response.OrderId);
        Assert.Equal(invoice.InvoiceNo, response.InvoiceNo);
        Assert.Equal(invoice.BuyerName, response.BuyerName);
        Assert.Equal(invoice.BuyerTaxCode, response.BuyerTaxCode);
        Assert.Equal(invoice.BuyerAddress, response.BuyerAddress);
        Assert.Equal(invoice.TotalBeforeTax, response.TotalBeforeTax);
        Assert.Equal(invoice.TaxAmount, response.TaxAmount);
        Assert.Equal(invoice.GrandTotal, response.GrandTotal);
        Assert.Equal(new DateTimeOffset(invoice.IssuedAt, TimeSpan.Zero), response.IssuedAt);
        var item = Assert.Single(invoice.Order.Items);
        Assert.Equal(new InvoiceItemResponse(item.SkuId, "Product", "INVOICE", 2, 100, 198), Assert.Single(response.Items));
    }

    [Fact]
    public async Task Detail_ReturnsTypedNotFound()
    {
        var result = await DetailHandler().Handle(new(Guid.NewGuid()), default);
        Assert.Equal(InvoiceReadErrors.InvoiceNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void DetailValidator_RejectsEmptyId()
    {
        var validator = new GetInvoiceByIdQueryValidator();
        Assert.False(validator.Validate(new GetInvoiceByIdQuery(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new GetInvoiceByIdQuery(Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_ChecksExplicitAccessToAnotherStore(bool allowed)
    {
        var invoice = SetUpInvoice(Guid.NewGuid());
        accesses.ExistsAsync(employee.Id, invoice.Order.StoreId, Arg.Any<CancellationToken>()).Returns(allowed);
        var result = await DetailHandler().Handle(new(invoice.Id), default);
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed) Assert.Equal(InvoiceReadErrors.InvalidStore, result.Error);
    }

    [Fact]
    public async Task Queries_AllowChainOwnerAcrossStores()
    {
        var owner = new Employee("Owner", "owner", "hash", "hash", Guid.NewGuid(), isChainOwner: true, id: employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var invoice = SetUpInvoice(Guid.NewGuid());
        Assert.True((await DetailHandler().Handle(new(invoice.Id), default)).IsSuccess);
        await accesses.DidNotReceive().ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        invoices.GetPagedAsync(owner.Id, null, true, null, null, null, 1, 20, default)
            .Returns((new List<InvoiceSummaryResponse>(), 0));
        Assert.True((await ListHandler().Handle(new(new()), default)).IsSuccess);
        await invoices.Received(1).GetPagedAsync(owner.Id, null, true, null, null, null, 1, 20, default);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("missing")]
    [InlineData("inactive")]
    public async Task Queries_RejectInvalidEmployeeBeforeReadingInvoices(string state)
    {
        if (state == "anonymous") user.EmployeeId.Returns((Guid?)null);
        if (state == "missing") employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns((Employee?)null);
        if (state == "inactive") employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(
            new Employee("Inactive", "inactive", "hash", "hash", Guid.NewGuid(), isActive: false, id: employee.Id));
        Assert.Equal(InvoiceReadErrors.Unauthorized, (await ListHandler().Handle(new(new()), default)).Error);
        Assert.Equal(InvoiceReadErrors.Unauthorized, (await DetailHandler().Handle(new(Guid.NewGuid()), default)).Error);
        Assert.Empty(invoices.ReceivedCalls());
    }

    [Theory]
    [InlineData(false, false, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    [InlineData(true, true, ErrorType.None)]
    public async Task Queries_RequireSeededOrdersReadPermission(bool authenticated, bool allowed, ErrorType expected)
    {
        user.IsAuthenticated.Returns(authenticated);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>())
            .Returns(allowed ? ["orders:read"] : ["orders:update"]);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(employees);
        services.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());
        await CheckPermission<GetInvoicesQuery, PagedResponse<InvoiceSummaryResponse>>(new(new()), services, expected);
        await CheckPermission<GetInvoiceByIdQuery, InvoiceDetailResponse>(new(Guid.NewGuid()), services, expected);
    }

    private async Task CheckPermission<TQuery, TResponse>(TQuery query, IServiceProvider services, ErrorType expected)
        where TQuery : IRequest<Result<TResponse>>, IRequirePermission
    {
        Assert.Equal("orders:read", query.RequiredPermission);
        var called = false;
        var result = await new AuthorizationBehavior<TQuery, Result<TResponse>>(user, services).Handle(query, () =>
        {
            called = true;
            return Task.FromResult(Result<TResponse>.Success(default!));
        }, default);
        Assert.Equal(expected == ErrorType.None, called);
        Assert.Equal(expected, result.Error.Type);
    }

    private Invoice SetUpInvoice(Guid storeId)
    {
        var order = Order.CreateDraft(storeId, Guid.NewGuid(), employee.Id);
        var product = new Product(storeId, Guid.NewGuid(), "Product", "piece");
        var sku = new Sku(product.Id, storeId, "INVOICE", "123", 100, 50, 10, true) { Product = product };
        order.AddOrUpdateItem(sku, 2).ApplyDiscountAndTax(20, 10);
        var invoice = Invoice.Create(order.Id, "HD-STORE-20260930-000001", 200, 18, 198, "Buyer", "TAX", "Address");
        typeof(Invoice).GetProperty(nameof(Invoice.Order))!.SetValue(invoice, order);
        invoices.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        return invoice;
    }
}
