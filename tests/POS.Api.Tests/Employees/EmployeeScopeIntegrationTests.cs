using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using static POS.Api.Tests.Employees.EmployeeRulesIntegrationTests;

namespace POS.Api.Tests.Employees;

public class EmployeeScopeIntegrationTests
{
    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Detail_refreshes_caller_tracked_before_transaction_so_hierarchy_is_current(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        await using var db = f.Db();
        var repository = new POS.Infrastructure.Persistence.Repositories.EmployeeRepository(db);
        var caller = await repository.GetByIdAsync(f.People["owner"].Id);
        Assert.Equal(f.Owner.Id, caller!.RoleId);
        await using (var other = f.Db())
            await other.Employees.Where(e => e.Id == caller.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(e => e.RoleId, f.Cashier.Id).SetProperty(e => e.StoreId, f.B.Id));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var refreshed = await repository.GetDetailAsync(caller.Id);
        Assert.Equal(f.Cashier.Id, refreshed!.RoleId);
        Assert.Equal(f.Cashier.Id, refreshed.Role.Id);
        Assert.Equal(f.B.Id, refreshed.StoreId);
        Assert.Equal(f.B.Id, refreshed.Store!.Id);
    }

    [EmployeePostgresTheory]
    [InlineData("owner")]
    [InlineData("manager")]
    [InlineData("cashier")]
    [InlineData("custom")]
    public async Task Store_scope_applies_to_detail_history_list_and_mutation(string caller)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, caller);
        using var detail = await client.GetAsync($"/api/v1/employees/{f.People["cashierB"].Id}");
        using var history = await client.GetAsync($"/api/v1/employees/{f.People["cashierB"].Id}/login-history");
        using var otherStore = await client.GetAsync($"/api/v1/employees?StoreId={f.B.Id}");
        using var update = await Update(client, f.People["cashierB"]);
        await Expect(detail, HttpStatusCode.Forbidden);
        await Expect(history, HttpStatusCode.Forbidden);
        await Expect(otherStore, HttpStatusCode.Forbidden);
        await Expect(update, HttpStatusCode.Forbidden);
        using var list = await client.GetAsync("/api/v1/employees");
        await Expect(list, HttpStatusCode.OK);
        var page = (await list.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>())!.Data!;
        Assert.All(page.Items, e => Assert.Equal(f.A.Id, e.StoreId));
        if (caller is "cashier" or "custom") Assert.Equal(f.People[caller].Id, Assert.Single(page.Items).Id);
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Null_store_is_accepted_only_for_chain_owner_creation(bool chain)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "chain");
        using var response = await client.PostAsJsonAsync("/api/v1/employees",
            new CreateEmployeeRequest("No store", "storeless", EmployeeApiFactory.Password, "654321", f.Owner.Id, null, chain));
        await Expect(response, chain ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
        if (chain)
        {
            var employee = (await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!;
            Assert.Null(employee.StoreId);
            Assert.True(employee.IsChainOwner);
        }
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task List_filters_and_paging_include_inactive_by_default_and_never_leak_credentials(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        await using (var db = f.Db())
        {
            var cashier = await db.Employees.SingleAsync(e => e.Id == f.People["cashier"].Id);
            cashier.SetActive(false);
            await db.SaveChangesAsync();
        }
        using var client = f.Client();
        await f.LoginAsync(client, "owner");
        using var list = await client.GetAsync($"/api/v1/employees?RoleId={f.Cashier.Id}&Search=cash&PageSize=1");
        await Expect(list, HttpStatusCode.OK);
        var raw = await list.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw);
        Assert.DoesNotContain("pinHash", raw);
        Assert.DoesNotContain("pinLookupHash", raw);
        var page = (await list.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>())!.Data!;
        Assert.False(Assert.Single(page.Items).IsActive);
        using var active = await client.GetAsync($"/api/v1/employees?RoleId={f.Cashier.Id}&IsActive=true");
        Assert.Empty((await active.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>())!.Data!.Items);
        using var invalidSize = await client.GetAsync("/api/v1/employees?PageSize=101");
        await Expect(invalidSize, HttpStatusCode.BadRequest);
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Audit_failure_rolls_back_employee_creation(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        await using (var db = f.Db())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_audit() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Injected audit failure'; END; $$;
                CREATE TRIGGER reject_audit BEFORE INSERT ON audit_logs
                FOR EACH ROW WHEN (NEW.action LIKE 'Employee.%') EXECUTE FUNCTION reject_audit();
                """);
        }
        using var client = f.Client();
        await f.LoginAsync(client, "owner");
        using var response = await Create(client, f);
        await Expect(response, HttpStatusCode.InternalServerError);
        await using var verify = f.Db();
        Assert.False(await verify.Employees.AnyAsync(e => e.Username == "newEmployee"));
        Assert.Empty(await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Cache_failure_after_commit_does_not_roll_back_profile_or_audit(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "owner");
        f.Cache.OnRemove = _ => throw new InvalidOperationException("Injected cache failure");
        using var response = await Update(client, f.People["cashier"], role: f.Manager.Id);
        await Expect(response, HttpStatusCode.OK);
        await using var verify = f.Db();
        Assert.Equal(f.Manager.Id, (await verify.Employees.SingleAsync(e => e.Id == f.People["cashier"].Id)).RoleId);
        Assert.Single(await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
    }
}
