using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using POS.Application;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Services;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.Strategies;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Employees;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Stores;

namespace POS.Infrastructure.Tests.Orders;

public class SplitPaymentPersistenceTests
{
    [PostgresFact]
    public async Task PaymentRepository_ReadsPaymentAndOrderWithoutTracking()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        var payment = new Payment(fixture.OrderId, PaymentMethod.Card, 40, "status-ref", PaymentStatus.Pending);
        await using (var seed = fixture.Db())
        {
            seed.Payments.Add(payment);
            await seed.SaveChangesAsync();
        }

        await using var db = fixture.Db();
        var repository = new PaymentRepository(db);
        var found = await repository.GetByIdWithOrderAsync(payment.Id);

        Assert.NotNull(found);
        Assert.Equal(fixture.OrderId, found.OrderId);
        Assert.Equal(fixture.OrderId, found.Order.Id);
        Assert.Equal(PaymentStatus.Pending, found.Status);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Null(await repository.GetByIdWithOrderAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Split_ShouldUseRealPromotionTotal_AndPersistRecalculatedDiscounts()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using (var seed = fixture.Db())
        {
            var order = (await new OrderRepository(seed).GetByIdWithDetailsAsync(fixture.OrderId))!;
            var promotion = new Promotion(order.StoreId, "Cart discount", PromotionType.CartFixed, 20,
                validFrom: DateTime.UtcNow.AddDays(-1), createdBy: order.CreatedBy);
            seed.Promotions.Add(promotion);
            await seed.SaveChangesAsync();
            var cart = new CartCalculationService(new SkuRepository(seed), new PromotionRepository(seed),
                new VoucherRepository(seed), new CustomerRepository(seed), new PromotionEngine());
            Assert.True((await cart.RecalculateAsync(order)).IsSuccess);
            seed.OrderDiscounts.AddRange(order.Discounts);
            await seed.SaveChangesAsync();
            Assert.Equal(80, order.GrandTotal);
        }
        await using (var db = fixture.Db())
        {
            var result = await fixture.Handler(db).Handle(new(fixture.OrderId, [new("Cash", 90)]), default);
            Assert.True(result.IsSuccess, result.Error.Code);
            Assert.Equal(80, result.Value!.TotalPaid);
            Assert.Equal(10, result.Value.ChangeAmount);
        }
        await using var verify = fixture.Db();
        var saved = (await new OrderRepository(verify).GetByIdWithDetailsAsync(fixture.OrderId))!;
        Assert.Equal(80, saved.GrandTotal);
        Assert.Single(saved.Discounts);
        Assert.Equal(20, saved.Discounts.Single().DiscountAmount);
    }

    [PostgresFact]
    public async Task Split_ShouldPersistPaymentsChangeAndPoints_AndCompleteOnlyOnce()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using (var first = fixture.Db())
        {
            var result = await fixture.Handler(first).Handle(new(fixture.OrderId,
                [new("Points", 30, "points"), new("Card", 30, "card")]), default);
            Assert.True(result.IsSuccess, result.Error.Code);
            Assert.Equal("Confirmed", result.Value!.Status);
            Assert.Equal(60, result.Value.TotalPaid);
        }
        await using (var second = fixture.Db())
        {
            var result = await fixture.Handler(second).Handle(new(fixture.OrderId, [new("Cash", 50)]), default);
            Assert.True(result.IsSuccess, result.Error.Code);
            Assert.Equal(100, result.Value!.TotalPaid);
            Assert.Equal(10, result.Value.ChangeAmount);
        }
        await using var verify = fixture.Db();
        var order = await new OrderRepository(verify).GetByIdWithDetailsAsync(fixture.OrderId);
        Assert.Equal(OrderStatus.Paid, order!.Status);
        Assert.Equal(3, order.Payments.Count);
        Assert.Equal(100, order.GetPaymentTotals().TotalApplied);
        Assert.Equal(170, (await verify.LoyaltyAccounts.SingleAsync()).PointsBalance);
        Assert.Equal(9, (await verify.StockEntries.SingleAsync()).QtyOnHand);
        Assert.Single(await verify.Invoices.ToListAsync());
        Assert.Single(await verify.StockTransactions.ToListAsync());
        var retry = await fixture.Handler(verify).Handle(new(fixture.OrderId, [new("Cash", 50)]), default);
        Assert.Equal("ORDER.ALREADY_PAID", retry.Error.Code);
    }

    [PostgresFact]
    public async Task PaidOrders_ShouldReceiveSequentialStoreLocalInvoiceNumbers()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using (var first = fixture.Db())
            Assert.True((await fixture.Handler(first).Handle(new(fixture.OrderId, [new("Cash", 100)]), default)).IsSuccess);
        await using (var second = fixture.Db())
            Assert.True((await fixture.Handler(second).Handle(new(fixture.SecondOrderId, [new("Cash", 100)]), default)).IsSuccess);

        await using var verify = fixture.Db();
        var store = await verify.Stores.SingleAsync();
        var invoices = await verify.Invoices.OrderBy(i => i.InvoiceNo).ToListAsync();
        Assert.Equal(2, invoices.Count);
        var date = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(store.Timezone)).ToString("yyyyMMdd");
        Assert.Equal($"HD-{store.Code}-{date}-000001", invoices[0].InvoiceNo);
        Assert.Equal($"HD-{store.Code}-{date}-000002", invoices[1].InvoiceNo);
        Assert.Equal(store.Id.ToString("N").ToUpperInvariant(), store.Code);
        Assert.Equal(2, (await verify.InvoiceSequences.SingleAsync()).LastValue);
    }

    [PostgresFact]
    public async Task ConcurrentPaidOrders_ShouldRetrySerializableConflict_AndGetDistinctNumbers()
    {
        await using var fixture = await PaymentDatabase.CreateAsync(confirmed: true);
        var results = await RunRace(fixture, fixture.OrderId, fixture.SecondOrderId,
            "invoice-one", "invoice-two", amount: 100);
        Assert.All(results, result => Assert.True(result.IsSuccess, result.Error.Code));
        await using var verify = fixture.Db();
        var invoices = await verify.Invoices.ToListAsync();
        Assert.Equal(2, invoices.Count);
        Assert.Equal(2, invoices.Select(i => i.InvoiceNo).Distinct().Count());
        Assert.Contains(invoices, i => i.InvoiceNo.EndsWith("-000001"));
        Assert.Contains(invoices, i => i.InvoiceNo.EndsWith("-000002"));
    }

    [PostgresFact]
    public async Task InvoiceSequence_ShouldRestartForAnotherStoreAndBusinessDate()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var secondStore = new Store("Another store");
        db.Stores.Add(secondStore);
        await db.SaveChangesAsync();
        var firstStoreId = (await db.Stores.SingleAsync(s => s.Id != secondStore.Id)).Id;
        var repository = new InvoiceRepository(db);
        var today = new DateOnly(2026, 9, 30);
        Assert.Equal(1, await repository.GetNextSequenceAsync(firstStoreId, today));
        Assert.Equal(2, await repository.GetNextSequenceAsync(firstStoreId, today));
        Assert.Equal(1, await repository.GetNextSequenceAsync(secondStore.Id, today));
        Assert.Equal(1, await repository.GetNextSequenceAsync(firstStoreId, today.AddDays(1)));
    }

    [PostgresFact]
    public async Task InvoiceUniqueIndexes_ShouldRejectDuplicateNumberAndOrder()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        var number = "HD-TEST-20260930-000001";
        await using (var first = fixture.Db())
        {
            first.Invoices.Add(Invoice.Create(fixture.OrderId, number, 100, 0, 100));
            await new UnitOfWork(first).SaveChangesAsync();
        }
        await using (var duplicateNumber = fixture.Db())
        {
            duplicateNumber.Invoices.Add(Invoice.Create(fixture.SecondOrderId, number, 100, 0, 100));
            var error = await Assert.ThrowsAsync<PersistenceConflictException>(() =>
                new UnitOfWork(duplicateNumber).SaveChangesAsync());
            Assert.Equal("IX_invoices_invoice_no", error.ConstraintName);
        }
        await using (var duplicateOrder = fixture.Db())
        {
            duplicateOrder.Invoices.Add(Invoice.Create(fixture.OrderId, "HD-OTHER-20260930-000001", 100, 0, 100));
            var error = await Assert.ThrowsAsync<PersistenceConflictException>(() =>
                new UnitOfWork(duplicateOrder).SaveChangesAsync());
            Assert.Equal("IX_invoices_order_id", error.ConstraintName);
        }
    }

    [PostgresFact]
    public async Task InvoiceSequence_ShouldExecuteOneReturningStatement_PerNumber()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        Guid storeId;
        await using (var read = fixture.Db()) storeId = (await read.Stores.SingleAsync()).Id;
        var commands = new CaptureCommands();
        await using var db = fixture.Db(commands);
        var repository = new InvoiceRepository(db);
        foreach (var expected in new long[] { 1, 2 })
        {
            commands.Sql.Clear();
            Assert.Equal(expected, await repository.GetNextSequenceAsync(storeId, new DateOnly(2026, 9, 30)));
            var sql = Assert.Single(commands.Sql);
            Assert.StartsWith("INSERT INTO invoice_sequences", sql.TrimStart());
            Assert.Contains("ON CONFLICT (store_id, invoice_date)", sql);
            Assert.Contains("RETURNING last_value AS \"Value\"", sql);
            Assert.DoesNotContain("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [PostgresFact]
    public async Task Generate_ShouldRejectPendingInvoice_InSameTransaction()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var result = await new UnitOfWork(db).ExecuteSerializableAsync<int>(async ct =>
        {
            var order = (await new OrderRepository(db).GetByIdWithDetailsAsync(fixture.OrderId, ct))!;
            order.Confirm();
            order.ProcessPayments([(PaymentMethod.Cash, 100, null)]);
            var handler = new GenerateInvoiceCommandHandler(new OrderRepository(db), new StoreRepository(db),
                new InvoiceRepository(db), Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateInvoiceCommandHandler>.Instance);
            Assert.True((await handler.Handle(new(order.Id), ct)).IsSuccess);
            var duplicate = await handler.Handle(new(order.Id), ct);
            Assert.Equal("INVOICE.ALREADY_EXISTS", duplicate.Error.Code);
            Assert.Single(db.Invoices.Local);
            Assert.Equal(1, (await db.InvoiceSequences.SingleAsync(ct)).LastValue);
            // Roll back this deliberately unsaved transaction.
            return Result<int>.Failure(duplicate.Error);
        });
        Assert.True(result.IsFailure);
    }

    [PostgresFact]
    public async Task Checkout_ShouldRollbackAllWrites_WhenNestedInvoiceReturnsFailure() =>
        await AssertInvoiceRollback(throwException: false);

    [PostgresFact]
    public async Task Checkout_ShouldRollbackAllWrites_WhenNestedInvoiceThrows() =>
        await AssertInvoiceRollback(throwException: true);

    private static async Task AssertInvoiceRollback(bool throwException)
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var behavior = new FailInvoiceAfterWriting(db, throwException);
        var handler = fixture.Handler(db, invoiceBehavior: behavior);
        var command = new CheckoutOrderCommand(fixture.OrderId, [new("Points", 100, "invoice-rollback")]);
        if (throwException)
            await Assert.ThrowsAsync<InjectedPaymentFailure>(() => handler.Handle(command, default));
        else
            Assert.Equal("Invoice.InjectedFailure", (await handler.Handle(command, default)).Error.Code);

        Assert.True(behavior.Written);
        Assert.Empty(db.ChangeTracker.Entries());
        await using var verify = fixture.Db();
        Assert.Empty(await verify.Payments.ToListAsync());
        Assert.Equal(OrderStatus.Draft, (await verify.Orders.SingleAsync(o => o.Id == fixture.OrderId)).Status);
        Assert.Null((await verify.Orders.SingleAsync(o => o.Id == fixture.OrderId)).PaidAt);
        Assert.Equal(200, (await verify.LoyaltyAccounts.SingleAsync()).PointsBalance);
        Assert.Equal(10, (await verify.StockEntries.SingleAsync()).QtyOnHand);
        Assert.Empty(await verify.StockTransactions.ToListAsync());
        Assert.Empty(await verify.Invoices.ToListAsync());
        Assert.Empty(await verify.InvoiceSequences.ToListAsync());
    }

    [PostgresFact]
    public async Task Checkout_ShouldPersistUtcInvoice_WhenStoreTimezoneIsUnknown()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var store = await db.Stores.SingleAsync();
        store.UpdateInfo(store.Name, null, null, "Unknown/InvoiceTimezone", "VND", null, null, null);
        await db.SaveChangesAsync();
        var before = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.True((await fixture.Handler(db).Handle(new(fixture.OrderId, [new("Cash", 100)]), default)).IsSuccess);
        var after = DateTime.UtcNow.ToString("yyyyMMdd");
        await using var verify = fixture.Db();
        var invoice = await verify.Invoices.SingleAsync();
        Assert.Contains(invoice.InvoiceNo, new[] { $"HD-{store.Code}-{before}-000001", $"HD-{store.Code}-{after}-000001" });
        Assert.Equal(OrderStatus.Paid, (await verify.Orders.SingleAsync(o => o.Id == fixture.OrderId)).Status);
    }

    [PostgresFact]
    public async Task Split_ShouldRollbackPaymentOrderPointsAndStock_WhenSaveFailsAfterWriting()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var failing = fixture.Db(new FailAfterSave());
        await Assert.ThrowsAsync<InjectedPaymentFailure>(() => fixture.Handler(failing).Handle(
            new(fixture.OrderId, [new("Points", 100, "rollback")]), default));
        Assert.Empty(failing.ChangeTracker.Entries());
        await using var verify = fixture.Db();
        Assert.Empty(await verify.Payments.ToListAsync());
        Assert.Equal(OrderStatus.Draft, (await verify.Orders.SingleAsync(o => o.Id == fixture.OrderId)).Status);
        Assert.Equal(200, (await verify.LoyaltyAccounts.SingleAsync()).PointsBalance);
        Assert.Equal(10, (await verify.StockEntries.SingleAsync()).QtyOnHand);
        Assert.Empty(await verify.Invoices.ToListAsync());
        Assert.Empty(await verify.InvoiceSequences.ToListAsync());
        Assert.Empty(await verify.StockTransactions.ToListAsync());
    }

    [PostgresFact]
    public async Task Split_ShouldRollbackDraftConfirmation_WhenValidationFails()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var result = await fixture.Handler(db).Handle(new(fixture.OrderId, [new("Card", 101, "over")]), default);
        Assert.Equal("ORDER.NON_CASH_OVERPAYMENT", result.Error.Code);
        Assert.Empty(db.ChangeTracker.Entries());
        await using var verify = fixture.Db();
        Assert.Empty(await verify.Payments.ToListAsync());
        Assert.Equal(OrderStatus.Draft, (await verify.Orders.SingleAsync(o => o.Id == fixture.OrderId)).Status);
    }

    [PostgresFact]
    public async Task Split_ShouldRejectConcurrentOverpayment_AndRecheckRemainingOnRetry()
    {
        await using var fixture = await PaymentDatabase.CreateAsync(confirmed: true);
        var results = await RunRace(fixture, fixture.OrderId, fixture.OrderId, "one", "two");
        Assert.Single(results, r => r.IsSuccess);
        Assert.Equal("ORDER.NON_CASH_OVERPAYMENT", results.Single(r => r.IsFailure).Error.Code);
        await using var verify = fixture.Db();
        Assert.Single(await verify.Payments.ToListAsync());
        Assert.Equal(60, await verify.Payments.SumAsync(p => p.Amount));
        var retry = await fixture.Handler(verify).Handle(new(fixture.OrderId, [new("Card", 60, "retry")]), default);
        Assert.Equal("ORDER.NON_CASH_OVERPAYMENT", retry.Error.Code);
    }

    [PostgresFact]
    public async Task Split_ShouldRejectConcurrentDuplicateAcrossOrders_AndRejectRetry()
    {
        await using var fixture = await PaymentDatabase.CreateAsync(confirmed: true);
        var results = await RunRace(fixture, fixture.OrderId, fixture.SecondOrderId, "shared", "shared");
        Assert.Single(results, r => r.IsSuccess);
        Assert.Contains(results.Single(r => r.IsFailure).Error.Code,
            new[] { "ORDER.DUPLICATE_PAYMENT", "Persistence.ConcurrentModification" });
        await using var verify = fixture.Db();
        Assert.Single(await verify.Payments.ToListAsync());
        var retry = await fixture.Handler(verify).Handle(new(fixture.SecondOrderId, [new("Card", 60, "shared")]), default);
        Assert.Equal("ORDER.DUPLICATE_PAYMENT", retry.Error.Code);
    }

    [PostgresFact]
    public async Task Split_ShouldPreventConcurrentDoubleSpendOfLoyaltyBalance()
    {
        await using var fixture = await PaymentDatabase.CreateAsync(confirmed: true);
        await using (var seed = fixture.Db())
        {
            (await seed.LoyaltyAccounts.SingleAsync()).SetPoints(100);
            await seed.SaveChangesAsync();
        }
        var results = await RunRace(fixture, fixture.OrderId, fixture.SecondOrderId, "points-a", "points-b", "Points");
        Assert.Single(results, r => r.IsSuccess);
        Assert.Equal("ORDER.INSUFFICIENT_POINTS", results.Single(r => r.IsFailure).Error.Code);
        await using var verify = fixture.Db();
        Assert.Equal(40, (await verify.LoyaltyAccounts.SingleAsync()).PointsBalance);
        Assert.Single(await verify.Payments.ToListAsync());
        var retry = await fixture.Handler(verify).Handle(new(fixture.OrderId, [new("Points", 60, "points-retry")]), default);
        Assert.Equal("ORDER.INSUFFICIENT_POINTS", retry.Error.Code);
    }

    [PostgresFact]
    public async Task Split_ShouldAllowNullReferences_AndPreserveCaseSensitiveReferences()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var result = await fixture.Handler(db).Handle(new(fixture.OrderId,
            [new("Cash", 10), new("Cash", 10), new("Card", 10, "REF"), new("Card", 10, "ref")]), default);
        Assert.True(result.IsSuccess, result.Error.Code);
        await using var verify = fixture.Db();
        Assert.Equal(4, await verify.Payments.CountAsync());
        Assert.Equal(40, await verify.Payments.SumAsync(p => p.Amount));
    }

    [PostgresFact]
    public async Task UniqueIndex_ShouldPreventDuplicateEvenWithoutApplicationPrecheck()
    {
        await using var fixture = await PaymentDatabase.CreateAsync();
        await using var db = fixture.Db();
        var uow = new UnitOfWork(db);
        db.Payments.Add(Payment.CreateSuccess(fixture.OrderId, PaymentMethod.Card, 10, "unique"));
        await uow.SaveChangesAsync();
        db.Payments.Add(Payment.CreateSuccess(fixture.SecondOrderId, PaymentMethod.Card, 10, "unique"));
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => uow.SaveChangesAsync());
        Assert.Equal("IX_payments_method_transaction_ref", conflict.ConstraintName);
        await using var verify = fixture.Db();
        Assert.Single(await verify.Payments.ToListAsync());
    }

    private static async Task<POS.Domain.Common.Result<POS.Application.UseCases.Orders.DTOs.CheckoutDto>[]> RunRace(
        PaymentDatabase fixture, Guid firstId, Guid secondId, string firstRef, string secondRef,
        string method = "Card", decimal amount = 60)
    {
        await using var a = fixture.Db();
        await using var b = fixture.Db();
        var readA = new PausedOrderRepository(new OrderRepository(a));
        var readB = new PausedOrderRepository(new OrderRepository(b));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var taskA = fixture.Handler(a, readA).Handle(new(firstId, [new(method, amount, firstRef)]), timeout.Token);
        var taskB = fixture.Handler(b, readB).Handle(new(secondId, [new(method, amount, secondRef)]), timeout.Token);
        try
        {
            await Task.WhenAll(readA.Read.Task, readB.Read.Task).WaitAsync(timeout.Token);
            readB.Continue.TrySetResult();
            var resultB = await taskB;
            readA.Continue.TrySetResult();
            return [await taskA, resultB];
        }
        finally
        {
            readA.Continue.TrySetResult();
            readB.Continue.TrySetResult();
            await timeout.CancelAsync();
            try { await Task.WhenAll(taskA, taskB); } catch (OperationCanceledException) { }
        }
    }

    private sealed class InjectedPaymentFailure : Exception;

    private sealed class FailInvoiceAfterWriting(AppDbContext db, bool throwException)
        : IPipelineBehavior<GenerateInvoiceCommand, Result>
    {
        public bool Written { get; private set; }

        public async Task<Result> Handle(GenerateInvoiceCommand request, RequestHandlerDelegate<Result> next,
            CancellationToken cancellationToken)
        {
            var transaction = db.Database.CurrentTransaction;
            Assert.NotNull(transaction);
            Assert.Equal(System.Data.IsolationLevel.Serializable, transaction.GetDbTransaction().IsolationLevel);
            var result = await next();
            Assert.True(result.IsSuccess, result.Error.Code);
            Assert.Same(transaction, db.Database.CurrentTransaction);
            // Flush every side effect before failure: absence of SaveChanges cannot mask a missing rollback.
            await db.SaveChangesAsync(cancellationToken);
            Assert.True(await db.Payments.AnyAsync(cancellationToken));
            Assert.True(await db.Invoices.AnyAsync(cancellationToken));
            Assert.Equal(OrderStatus.Paid, (await db.Orders.SingleAsync(o => o.Id == request.OrderId, cancellationToken)).Status);
            Assert.Equal(100, (await db.LoyaltyAccounts.SingleAsync(cancellationToken)).PointsBalance);
            Assert.Equal(9, (await db.StockEntries.AsNoTracking().SingleAsync(cancellationToken)).QtyOnHand);
            Written = true;
            if (throwException) throw new InjectedPaymentFailure();
            return Result.Failure(new Error(ErrorType.Invalid, "Invoice.InjectedFailure", "Injected failure after writing invoice."));
        }
    }

    private sealed class CaptureCommands : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default) => throw new InjectedPaymentFailure();
    }

    private sealed class PausedOrderRepository(IOrderRepository inner) : IOrderRepository
    {
        public TaskCompletionSource Read { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await inner.GetByIdWithDetailsAsync(id, cancellationToken);
            Read.TrySetResult();
            await Continue.Task.WaitAsync(cancellationToken);
            return order;
        }
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public Task<bool> PaymentReferenceExistsAsync(PaymentMethod method, string transactionRef, CancellationToken cancellationToken = default) => inner.PaymentReferenceExistsAsync(method, transactionRef, cancellationToken);
        public Task AddAsync(Order order, CancellationToken cancellationToken = default) => inner.AddAsync(order, cancellationToken);
        public Task AddPaymentsAsync(IEnumerable<Payment> payments, CancellationToken cancellationToken = default) => inner.AddPaymentsAsync(payments, cancellationToken);
        public void Update(Order order) => inner.Update(order);
        public void ReplaceDiscounts(IEnumerable<OrderDiscount> previous, IEnumerable<OrderDiscount> current) => inner.ReplaceDiscounts(previous, current);
    }

    private sealed class PaymentDatabase : IAsyncDisposable
    {
        private readonly string schema = "split_payment_test_" + Guid.NewGuid().ToString("N");
        private string connection = null!;
        private Guid employeeId;
        private readonly List<ServiceProvider> providers = [];
        public Guid OrderId { get; private set; }
        public Guid SecondOrderId { get; private set; }

        public static async Task<PaymentDatabase> CreateAsync(bool confirmed = false)
        {
            var fixture = new PaymentDatabase();
            fixture.connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES"))
            { SearchPath = fixture.schema, Pooling = false }.ConnectionString;
            await using var admin = new NpgsqlConnection(fixture.connection);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{fixture.schema}\"", admin);
            await create.ExecuteNonQueryAsync();
            try
            {
                await using var db = fixture.Db();
                await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
                var store = new Store("Payment store");
                var role = new Role(RoleNames.Cashier, true);
                var employee = new Employee("Cashier", "payment_cashier", "unused", "unused", role.Id, storeId: store.Id);
                employee.setPinLookUpHash(new string('a', 64));
                fixture.employeeId = employee.Id;
                var shift = Shift.Open(store.Id, employee.Id, 0, null);
                var category = Category.Create(store.Id, "Category");
                var product = new Product(store.Id, category.Id, "Product", "piece");
                var sku = new Sku(product.Id, store.Id, "PAY-SKU", "123456789", 100, 50, 0, true) { Product = product };
                var tier = new MemberTier(MemberTierName.Normal, 0, 0, 0);
                var customer = new Customer("Customer", "0123456789", tier.Id);
                var order = Order.CreateDraft(store.Id, shift.Id, employee.Id, customer.Id);
                var second = Order.CreateDraft(store.Id, shift.Id, employee.Id, customer.Id);
                foreach (var target in new[] { order, second })
                {
                    target.AddOrUpdateItem(sku, 1);
                    target.ApplyPromotionEvaluation(new PromotionResult(100, 0, 100, [], [], []), new Dictionary<Guid, decimal>());
                    if (confirmed) target.Confirm();
                }
                fixture.OrderId = order.Id;
                fixture.SecondOrderId = second.Id;
                db.AddRange(store, role, employee, shift, category, product, sku, tier, customer,
                    new LoyaltyAccount(customer.Id, 200), new StockEntry(store.Id, sku.Id, 10), order, second);
                await db.SaveChangesAsync();
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }

        public AppDbContext Db(params IInterceptor[] interceptors) => new(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).AddInterceptors(interceptors).Options);

        public CheckoutOrderCommandHandler Handler(AppDbContext db, IOrderRepository? orders = null,
            IPipelineBehavior<GenerateInvoiceCommand, Result>? invoiceBehavior = null)
        {
            var user = Substitute.For<ICurrentUser>();
            user.EmployeeId.Returns(employeeId);
            var customers = new CustomerRepository(db);
            var vouchers = new VoucherRepository(db);
            var strategies = new PaymentStrategyFactory([
                new CashPaymentStrategy(), new CardPaymentStrategy(), new MoMoPaymentStrategy(),
                new VietQrPaymentStrategy(), new PointsPaymentStrategy(customers)]);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddSingleton(user);
            services.AddSingleton<IOrderRepository>(orders ?? new OrderRepository(db));
            services.AddSingleton<IStoreRepository>(new StoreRepository(db));
            services.AddSingleton<IInvoiceRepository>(new InvoiceRepository(db));
            if (invoiceBehavior is not null) services.AddSingleton(invoiceBehavior);
            var provider = services.BuildServiceProvider();
            providers.Add(provider);
            return new(orders ?? new OrderRepository(db), new ShiftRepository(db), new EmployeeRepository(db),
                new EmployeeStoreAccessRepository(db), new StoreRepository(db), new StockEntryRepository(db),
                new StockTransactionRepository(db), provider.GetRequiredService<ISender>(), vouchers, new VoucherUsageRepository(db),
                strategies, new CartCalculationService(new SkuRepository(db), new PromotionRepository(db), vouchers,
                    customers, new PromotionEngine()), new UnitOfWork(db), user, customers);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var provider in providers) await provider.DisposeAsync();
            await using var admin = new NpgsqlConnection(connection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
