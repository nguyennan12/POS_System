using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Commands.AssignAdminToStore;
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;
using POS.Application.UseCases.Stores.Commands.UpdateStore;
using POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;
using POS.Application.UseCases.Stores.Queries.GetAllStores;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Application.Tests.Stores;

public class StoreManagementTests
{
    private readonly IStoreRepository stores = Substitute.For<IStoreRepository>();
    private readonly IEmployeeRepository employees = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeStoreAccessRepository access = Substitute.For<IEmployeeStoreAccessRepository>();
    private readonly IRoleRepository roles = Substitute.For<IRoleRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService cache = Substitute.For<ICacheService>();
    private readonly Store store = new("Store", phone: "0123456789", timezone: "UTC", currencyCode: "USD",
        taxCode: "TAX", receiptHeader: "Header", receiptFooter: "Footer");
    private readonly Employee owner;

    public StoreManagementTests()
    {
        owner = EmployeeWithRole(RoleNames.Owner, chainOwner: true);
        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(owner.Id);
        employees.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        stores.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        access.ExistsAsync(owner.Id, store.Id, Arg.Any<CancellationToken>()).Returns(true);
        unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<Result<StoreDetailDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<Result<StoreDetailDto>>>>(0)(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task Anonymous_list_does_not_query_stores()
    {
        currentUser.IsAuthenticated.Returns(false);
        var result = await new GetAllStoresQueryHandler(stores, employees, currentUser).Handle(new(), default);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Empty(stores.ReceivedCalls());
    }

    [Fact]
    public async Task StoreManager_cannot_list_owner_management_data()
    {
        var manager = EmployeeWithRole(RoleNames.StoreManager, store.Id);
        employees.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(manager);
        var result = await new GetAllStoresQueryHandler(stores, employees, currentUser).Handle(new(), default);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Empty(stores.ReceivedCalls());
    }

    [Fact]
    public async Task List_passes_database_scope_and_never_calls_unrestricted_GetAll()
    {
        stores.GetAccessibleAsync(owner.Id, true, null, Arg.Any<CancellationToken>()).Returns([store]);
        var result = await new GetAllStoresQueryHandler(stores, employees, currentUser).Handle(new(), default);
        Assert.Equal(store.Id, Assert.Single(result.Value!).Id);
        await stores.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Detail_without_access_is_forbidden_before_store_lookup()
    {
        access.ExistsAsync(owner.Id, store.Id, Arg.Any<CancellationToken>()).Returns(false);
        var result = await new GetStoreDetailQueryHandler(stores, employees, access, currentUser).Handle(new(store.Id), default);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        await stores.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Detail_contains_actual_settings_and_timestamps()
    {
        var result = await new GetStoreDetailQueryHandler(stores, employees, access, currentUser).Handle(new(store.Id), default);
        Assert.Equal("USD", result.Value!.CurrencyCode);
        Assert.Equal("UTC", result.Value.Timezone);
        Assert.Equal("0123456789", result.Value.Phone);
        Assert.Equal("TAX", result.Value.TaxCode);
        Assert.Equal("Footer", result.Value.ReceiptFooter);
        Assert.Equal(new DateTimeOffset(store.CreatedAt), result.Value.CreatedAt);
    }

    [Fact]
    public async Task Missing_store_returns_not_found_without_saving()
    {
        stores.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns((Store?)null);
        var result = await new UpdateStoreStatusCommandHandler(stores, employees, access, currentUser, unitOfWork)
            .Handle(new(store.Id, false), default);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Status_can_deactivate_then_reactivate()
    {
        var handler = new UpdateStoreStatusCommandHandler(stores, employees, access, currentUser, unitOfWork);
        Assert.False((await handler.Handle(new(store.Id, false), default)).Value!.IsActive);
        Assert.True((await handler.Handle(new(store.Id, true), default)).Value!.IsActive);
    }

    [Fact]
    public async Task Create_saves_store_and_creator_access_with_one_commit()
    {
        Store? created = null;
        EmployeeStoreAccess? granted = null;
        stores.AddAsync(Arg.Do<Store>(s => created = s), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        access.AddAsync(Arg.Do<EmployeeStoreAccess>(a => granted = a), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await new CreateStoreCommandHandler(stores, unitOfWork, employees, access, currentUser)
            .Handle(new("New", null, null, "UTC", "USD"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(created!.Id, granted!.StoreId);
        Assert.Equal(owner.Id, granted.EmployeeId);
        Assert.Equal(owner.Id, granted.GrantedBy);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_records_authenticated_grantor_without_changing_recipient_role()
    {
        var recipient = EmployeeWithRole(RoleNames.Owner, chainOwner: true);
        employees.GetByIdAsync(recipient.Id, Arg.Any<CancellationToken>()).Returns(recipient);
        var originalRoleId = recipient.RoleId;
        var result = await GrantHandler().Handle(new(store.Id, recipient.Id), default);
        Assert.True(result.IsSuccess);
        await access.Received(1).AddAsync(Arg.Is<EmployeeStoreAccess>(a =>
            a.EmployeeId == recipient.Id && a.StoreId == store.Id && a.GrantedBy == owner.Id), Arg.Any<CancellationToken>());
        Assert.Equal(originalRoleId, recipient.RoleId);
        Assert.Null(recipient.StoreId);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_rejects_non_owner_recipient()
    {
        var recipient = EmployeeWithRole(RoleNames.Cashier, store.Id);
        employees.GetByIdAsync(recipient.Id, Arg.Any<CancellationToken>()).Returns(recipient);
        var result = await GrantHandler().Handle(new(store.Id, recipient.Id), default);
        Assert.Equal(ErrorType.Invalid, result.Error.Type);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Duplicate_grant_returns_conflict_including_concurrent_insert(bool concurrent)
    {
        var recipient = EmployeeWithRole(RoleNames.Owner, chainOwner: true);
        employees.GetByIdAsync(recipient.Id, Arg.Any<CancellationToken>()).Returns(recipient);
        access.ExistsAsync(recipient.Id, store.Id, Arg.Any<CancellationToken>()).Returns(!concurrent);
        if (concurrent)
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(new DuplicateStoreAccessException(new Exception())));
        var result = await GrantHandler().Handle(new(store.Id, recipient.Id), default);
        Assert.Equal(ErrorType.AlreadyExists, result.Error.Type);
    }

    [Fact]
    public async Task Assign_promotes_cashier_and_transfers_store_in_one_commit()
    {
        var employee = PrepareCashier();
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(store.Id, employee.StoreId);
        Assert.Equal(RoleNames.StoreManager, employee.Role.Name);
        Assert.Equal(employee.Role.Id, employee.RoleId);
        Assert.False(employee.IsChainOwner);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"perm:{employee.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_requires_access_to_source_store_too()
    {
        var employee = PrepareCashier();
        var source = employee.StoreId;
        access.ExistsAsync(owner.Id, source!.Value, Arg.Any<CancellationToken>()).Returns(false);
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal(source, employee.StoreId);
        Assert.Equal(RoleNames.Cashier, employee.Role.Name);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_rejects_transfer_with_open_shift()
    {
        var employee = PrepareCashier();
        employees.HasOpenShiftOutsideStoreAsync(employee.Id, store.Id, Arg.Any<CancellationToken>()).Returns(true);
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.Equal("EmployeeOpenShift.Invalid", result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("VNDX")]
    [InlineData("VND\n")]
    [InlineData("usd")]
    public void Validators_reject_currency_that_does_not_fit_schema(string currency)
    {
        Assert.False(new CreateStoreCommandValidator().Validate(new CreateStoreCommand("Store", null, null, "UTC", currency)).IsValid);
        Assert.False(new UpdateStoreCommandValidator().Validate(
            new UpdateStoreCommand(store.Id, "Store", null, null, "UTC", currency, null, null, null)).IsValid);
    }

    private Employee PrepareCashier()
    {
        var source = Guid.NewGuid();
        var employee = EmployeeWithRole(RoleNames.Cashier, source);
        typeof(Employee).GetProperty(nameof(Employee.PinLookupHash))!.SetValue(employee, new string('a', 64));
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        access.ExistsAsync(owner.Id, source, Arg.Any<CancellationToken>()).Returns(true);
        roles.GetSystemRolesByNameAsync(RoleNames.StoreManager, Arg.Any<CancellationToken>())
            .Returns([new Role(RoleNames.StoreManager, isSystemRole: true)]);
        return employee;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Transfer_rejects_missing_lookup_without_changing_credentials_or_assignment(string? lookup)
    {
        var employee = PrepareCashier();
        typeof(Employee).GetProperty(nameof(Employee.PinLookupHash))!.SetValue(employee, lookup);
        var source = employee.StoreId;
        var roleId = employee.RoleId;
        var pinHash = employee.PinHash;
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);

        Assert.Equal("Employee.PinLookupMissing", result.Error.Code);
        Assert.Equal(source, employee.StoreId);
        Assert.Equal(roleId, employee.RoleId);
        Assert.Equal(pinHash, employee.PinHash);
        Assert.Equal(lookup, employee.PinLookupHash);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transfer_rejects_destination_with_unknown_pin_lookup()
    {
        var employee = PrepareCashier();
        employees.HasMissingPinLookupAsync(store.Id, Arg.Any<CancellationToken>()).Returns(true);
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.Equal("Employee.PinLookupMissing", result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Same_store_promotion_does_not_require_new_pin_lookup()
    {
        var employee = EmployeeWithRole(RoleNames.Cashier, store.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        roles.GetSystemRolesByNameAsync(RoleNames.StoreManager, Arg.Any<CancellationToken>())
            .Returns([new Role(RoleNames.StoreManager, isSystemRole: true)]);
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(RoleNames.StoreManager, employee.Role.Name);
        Assert.Null(employee.PinLookupHash);
        await employees.DidNotReceive().HasMissingPinLookupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transfer_still_rejects_duplicate_known_lookup()
    {
        var employee = PrepareCashier();
        employees.HasPinConflictAsync(employee.Id, store.Id, employee.PinLookupHash!, Arg.Any<CancellationToken>()).Returns(true);
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.Equal("EmployeePin.AlreadyExists", result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transaction_failure_does_not_invalidate_cache_or_report_success()
    {
        var employee = PrepareCashier();
        unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<Result<StoreDetailDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(Result<StoreDetailDto>.Failure(new Error(ErrorType.Invalid, "Persistence.ConcurrentModification")));
        var result = await AssignHandler().Handle(new(store.Id, employee.Id), default);
        Assert.Equal("Persistence.ConcurrentModification", result.Error.Code);
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private GrantOwnerAccessCommandHandler GrantHandler() => new(stores, employees, access, currentUser, unitOfWork);
    private AssignAdminToStoreCommandHandler AssignHandler() =>
        new(stores, employees, roles, access, currentUser, unitOfWork, cache, NullLogger<AssignAdminToStoreCommandHandler>.Instance);

    private static Employee EmployeeWithRole(string name, Guid? storeId = null, bool chainOwner = false)
    {
        var role = new Role(name, isSystemRole: true);
        var employee = new Employee("Employee", Guid.NewGuid().ToString(), "hash", "hash", role.Id, chainOwner, storeId);
        // Emulate the navigation populated by EF's Include(e => e.Role).
        typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(employee, role);
        return employee;
    }
}
