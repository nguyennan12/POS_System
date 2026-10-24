using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Reports;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Invoices.DTOs;
using POS.Application.UseCases.Invoices.Queries.GetReceiptData;
using POS.Application.UseCases.Invoices.Queries.GetInvoicePdf;
using POS.Application.UseCases.Invoices.Queries.GetEscPosReceipt;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Application.Tests.Invoices;

public class InvoiceReceiptQueryTests
{
    [Fact]
    public async Task Receipt_UsesOriginalCashierInvoiceSnapshotSuccessfulPaymentsAndStoreTimezone()
    {
        using var fixture = new Fixture();
        var result = await fixture.Sender.Send(new GetReceiptDataQuery(fixture.Invoice.Id, Reprint: true));
        Assert.True(result.IsSuccess);
        var data = result.Value!;
        Assert.Equal(58, data.PaperWidth);
        Assert.True(data.IsReprint);
        Assert.Equal(fixture.Invoice.InvoiceNo, data.InvoiceNo);
        Assert.Equal("Thu ngân gốc", data.Receipt.CashierName);
        Assert.NotEqual(fixture.Employee.Name, data.Receipt.CashierName);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.FromHours(7)), data.IssuedAt);
        Assert.Equal(100, data.Receipt.GrandTotal);
        Assert.Equal(100, data.Receipt.AmountPaid);
        Assert.Equal(100, data.Receipt.ChangeAmount);
        Assert.Equal(200, Assert.Single(data.Payments).Amount);
        Assert.Null(data.BuyerName);
        Assert.Null(data.BuyerTaxCode);
        Assert.Null(data.BuyerAddress);
        Assert.Equal("Cà phê", Assert.Single(data.Receipt.Items).ProductName);
        Assert.Empty(fixture.Pdf.ReceivedCalls());
        Assert.Empty(fixture.Thermal.ReceivedCalls());
        Assert.All(fixture.Invoices.ReceivedCalls(), c => Assert.Equal("GetForReceiptAsync", c.GetMethodInfo().Name));
    }

    [Theory]
    [InlineData("receipt")]
    [InlineData("pdf")]
    [InlineData("escpos")]
    public async Task AllQueries_RejectOtherStoreBeforeRendering_EvenWhenTokenClaimsOwner(string type)
    {
        using var fixture = new Fixture();
        fixture.User.IsChainOwner.Returns(true);
        fixture.User.StoreId.Returns(fixture.Invoice.Order.StoreId);
        fixture.Employees.GetByIdAsync(fixture.Employee.Id, Arg.Any<CancellationToken>()).Returns(
            new Employee("Moved employee", "moved", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid(), id: fixture.Employee.Id));
        var result = await Execute(fixture, type);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("INVOICE.INVALID_STORE", result.Error.Code);
        Assert.Empty(fixture.Pdf.ReceivedCalls());
        Assert.Empty(fixture.Thermal.ReceivedCalls());
    }

    [Theory]
    [InlineData("receipt", false)]
    [InlineData("pdf", false)]
    [InlineData("escpos", false)]
    [InlineData("receipt", true)]
    [InlineData("pdf", true)]
    [InlineData("escpos", true)]
    public async Task AllQueries_RequireAuthenticatedEmployeeAndInvoicePermission(string type, bool authenticated)
    {
        using var fixture = new Fixture();
        fixture.User.IsAuthenticated.Returns(authenticated);
        fixture.Cache.GetAsync<string[]>($"perm:{fixture.Employee.Id}", Arg.Any<CancellationToken>()).Returns(["orders:read"]);
        var result = await Execute(fixture, type);
        Assert.Equal(authenticated ? ErrorType.Forbidden : ErrorType.Unauthorized, result.Error.Type);
        Assert.Empty(fixture.Invoices.ReceivedCalls());
        Assert.Empty(fixture.Pdf.ReceivedCalls());
        Assert.Empty(fixture.Thermal.ReceivedCalls());
    }

    [Fact]
    public async Task PersistedChainOwner_CanCrossStore_AndDownloadAndReprintStayReadOnly()
    {
        using var fixture = new Fixture();
        fixture.Employees.GetByIdAsync(fixture.Employee.Id, Arg.Any<CancellationToken>()).Returns(
            new Employee("Owner", "owner", "hash", "hash", Guid.NewGuid(), isChainOwner: true, id: fixture.Employee.Id));
        var metadata = await fixture.Sender.Send(new GetInvoicePdfQuery(fixture.Invoice.Id, Reprint: true));
        Assert.True(metadata.IsSuccess);
        Assert.Null(metadata.Value!.FileBytes);
        Assert.Empty(fixture.Pdf.ReceivedCalls());
        Assert.True((await fixture.Sender.Send(new GetInvoicePdfQuery(fixture.Invoice.Id, true, true))).IsSuccess);
        fixture.Pdf.Received(1).Render(Arg.Is<InvoiceReceiptDto>(x => x.IsReprint && x.InvoiceNo == fixture.Invoice.InvoiceNo), Arg.Any<CancellationToken>());
        Assert.True((await fixture.Sender.Send(new GetEscPosReceiptQuery(fixture.Invoice.Id, 80, true))).IsSuccess);
        fixture.Thermal.Received(1).Render(Arg.Is<InvoiceReceiptDto>(x => x.IsReprint && x.PaperWidth == 80), 80, Arg.Any<CancellationToken>());
        Assert.All(fixture.Invoices.ReceivedCalls(), c => Assert.Equal("GetForReceiptAsync", c.GetMethodInfo().Name));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("locked")]
    public async Task InvalidPersistedEmployee_IsDeniedBeforeInvoiceRead(string state)
    {
        using var fixture = new Fixture();
        Employee? employee = state == "missing" ? null : new Employee("Invalid", "invalid", "hash", "hash", Guid.NewGuid(),
            isActive: state != "inactive", storeId: fixture.Employee.StoreId, id: fixture.Employee.Id);
        if (state == "locked") typeof(Employee).GetProperty(nameof(Employee.LockedUntil))!.SetValue(employee, DateTime.UtcNow.AddHours(1));
        fixture.Employees.GetByIdAsync(fixture.Employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var result = await fixture.Sender.Send(new GetReceiptDataQuery(fixture.Invoice.Id));
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Empty(fixture.Invoices.ReceivedCalls());
    }

    [Fact]
    public async Task UnknownInvoiceAndInvalidTimezone_ReturnTypedErrorsWithoutRendering()
    {
        using var fixture = new Fixture();
        var missing = await fixture.Sender.Send(new GetInvoicePdfQuery(Guid.NewGuid(), Download: true));
        Assert.Equal(ErrorType.NotFound, missing.Error.Type);
        typeof(Store).GetProperty(nameof(Store.Timezone))!.SetValue(fixture.Invoice.Order.Store, "Invalid/Timezone");
        var timezone = await fixture.Sender.Send(new GetEscPosReceiptQuery(fixture.Invoice.Id));
        Assert.Equal("INVOICE.INVALID_TIMEZONE", timezone.Error.Code);
        Assert.Empty(fixture.Pdf.ReceivedCalls());
        Assert.Empty(fixture.Thermal.ReceivedCalls());
    }

    [Fact]
    public async Task RenderingFailure_ReturnsSanitizedResult()
    {
        using var fixture = new Fixture();
        fixture.Pdf.Render(Arg.Any<InvoiceReceiptDto>(), Arg.Any<CancellationToken>()).Returns(_ => throw new ReceiptRenderingException());
        var result = await fixture.Sender.Send(new GetInvoicePdfQuery(fixture.Invoice.Id, Download: true));
        Assert.Equal("INVOICE.RENDERING_FAILED", result.Error.Code);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(58, true)]
    [InlineData(80, true)]
    [InlineData(0, false)]
    [InlineData(57, false)]
    [InlineData(81, false)]
    public void Validators_OnlyAcceptSupportedWidths(int? width, bool valid)
    {
        var id = Guid.NewGuid();
        Assert.Equal(valid, new GetReceiptDataQueryValidator().Validate(new GetReceiptDataQuery(id, width)).IsValid);
        Assert.Equal(valid, new GetEscPosReceiptQueryValidator().Validate(new GetEscPosReceiptQuery(id, width)).IsValid);
        Assert.False(new GetInvoicePdfQueryValidator().Validate(new GetInvoicePdfQuery(Guid.Empty)).IsValid);
        Assert.False(new GetReceiptDataQueryValidator().Validate(new GetReceiptDataQuery(Guid.Empty)).IsValid);
        Assert.False(new GetEscPosReceiptQueryValidator().Validate(new GetEscPosReceiptQuery(Guid.Empty)).IsValid);
    }

    private static async Task<Result> Execute(Fixture fixture, string type) => type switch
    {
        "pdf" => await fixture.Sender.Send(new GetInvoicePdfQuery(fixture.Invoice.Id, Download: true)),
        "escpos" => await fixture.Sender.Send(new GetEscPosReceiptQuery(fixture.Invoice.Id)),
        _ => await fixture.Sender.Send(new GetReceiptDataQuery(fixture.Invoice.Id))
    };

    private sealed class Fixture : IDisposable
    {
        private readonly IServiceProvider provider = Substitute.For<IServiceProvider>();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public IInvoiceRepository Invoices { get; } = Substitute.For<IInvoiceRepository>();
        public IEmployeeRepository Employees { get; } = Substitute.For<IEmployeeRepository>();
        public ICurrentUser User { get; } = Substitute.For<ICurrentUser>();
        public ICacheService Cache { get; } = Substitute.For<ICacheService>();
        public IInvoicePdfRenderer Pdf { get; } = Substitute.For<IInvoicePdfRenderer>();
        public IThermalReceiptRenderer Thermal { get; } = Substitute.For<IThermalReceiptRenderer>();
        public Employee Employee { get; } = new("Người in lại", "reprint", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());
        public Invoice Invoice { get; }

        public Fixture()
        {
            User.IsAuthenticated.Returns(true);
            User.EmployeeId.Returns(Employee.Id);
            Employees.GetByIdAsync(Employee.Id, Arg.Any<CancellationToken>()).Returns(Employee);
            Cache.GetAsync<string[]>($"perm:{Employee.Id}", Arg.Any<CancellationToken>()).Returns(["invoices:read"]);
            var store = new Store("Cửa hàng", timezone: "Asia/Ho_Chi_Minh", id: Employee.StoreId);
            var cashier = new Employee("Thu ngân gốc", "cashier", "hash", "hash", Guid.NewGuid(), storeId: store.Id);
            var order = Order.CreateDraft(store.Id, Guid.NewGuid(), cashier.Id);
            typeof(Order).GetProperty(nameof(Order.Store))!.SetValue(order, store);
            typeof(Order).GetProperty(nameof(Order.CreatedByEmployee))!.SetValue(order, cashier);
            var product = new Product(store.Id, Guid.NewGuid(), "Cà phê", "ly");
            var sku = new Sku(product.Id, store.Id, "CAFE", "123", 100, 50, 0, true) { Product = product };
            order.AddOrUpdateItem(sku, 1);
            order.Payments.Add(Payment.CreateSuccess(order.Id, PaymentMethod.Cash, 200, changeAmount: 100));
            order.Payments.Add(new Payment(order.Id, PaymentMethod.MoMo, 999, status: PaymentStatus.Pending));
            order.Payments.Add(new Payment(order.Id, PaymentMethod.Card, 999, status: PaymentStatus.Failed));
            Invoice = Invoice.Create(order.Id, "HD-TEST-20261024-000001", 100, 0, 100);
            typeof(Invoice).GetProperty(nameof(Invoice.Order))!.SetValue(Invoice, order);
            typeof(Invoice).GetProperty(nameof(Invoice.IssuedAt))!.SetValue(Invoice, new DateTime(2026, 10, 24, 18, 30, 0, DateTimeKind.Utc));
            Invoices.GetForReceiptAsync(Invoice.Id, Arg.Any<CancellationToken>()).Returns(Invoice);
            Pdf.Render(Arg.Any<InvoiceReceiptDto>(), Arg.Any<CancellationToken>()).Returns("%PDF-test"u8.ToArray());
            Thermal.Render(Arg.Any<InvoiceReceiptDto>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([0x1b, 0x40]);
            provider.GetService(typeof(ICacheService)).Returns(Cache);
            provider.GetService(typeof(IEmployeeRepository)).Returns(Employees);
            provider.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());
            Sender.Send(Arg.Any<GetReceiptDataQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var query = call.Arg<GetReceiptDataQuery>();
                var ct = call.Arg<CancellationToken>();
                return Authorize<GetReceiptDataQuery, InvoiceReceiptDto>(query, ct,
                    () => new GetReceiptDataQueryHandler(Invoices, Employees, User, new ReceiptSettings(58)).Handle(query, ct));
            });
            Sender.Send(Arg.Any<GetInvoicePdfQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var query = call.Arg<GetInvoicePdfQuery>();
                var ct = call.Arg<CancellationToken>();
                return Authorize<GetInvoicePdfQuery, InvoicePdfDto>(query, ct,
                    () => new GetInvoicePdfQueryHandler(Sender, Pdf).Handle(query, ct));
            });
            Sender.Send(Arg.Any<GetEscPosReceiptQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var query = call.Arg<GetEscPosReceiptQuery>();
                var ct = call.Arg<CancellationToken>();
                return Authorize<GetEscPosReceiptQuery, byte[]>(query, ct,
                    () => new GetEscPosReceiptQueryHandler(Sender, Thermal).Handle(query, ct));
            });
        }
        private Task<Result<T>> Authorize<TQuery, T>(TQuery query, CancellationToken ct, RequestHandlerDelegate<Result<T>> next)
            where TQuery : IRequest<Result<T>>, IRequirePermission =>
            new AuthorizationBehavior<TQuery, Result<T>>(User, provider).Handle(query, next, ct);
        public void Dispose() { }
    }
}
