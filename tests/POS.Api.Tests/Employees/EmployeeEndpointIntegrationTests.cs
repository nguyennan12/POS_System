using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using POS.Domain.Employees;
using POS.Domain.Stores;
using static POS.Api.Tests.Employees.EmployeeRulesIntegrationTests;

namespace POS.Api.Tests.Employees;

public class EmployeeEndpointIntegrationTests
{
    [EmployeePostgresTheory]
    [InlineData("list")]
    [InlineData("detail")]
    [InlineData("history")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("lock")]
    [InlineData("password")]
    [InlineData("pin")]
    public async Task Every_endpoint_requires_real_JWT_and_required_permission(string endpoint)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        async Task<HttpResponseMessage> Send() => endpoint switch
        {
            "list" => await client.GetAsync("/api/v1/employees"),
            "detail" => await client.GetAsync($"/api/v1/employees/{f.People["cashier"].Id}"),
            "history" => await client.GetAsync($"/api/v1/employees/{f.People["cashier"].Id}/login-history"),
            "create" => await Create(client, f),
            "update" => await Update(client, f.People["cashier"]),
            "lock" => await client.PutAsJsonAsync($"/api/v1/employees/{f.People["cashier"].Id}/lock", new LockEmployeeRequest(false)),
            "password" => await client.PostAsJsonAsync($"/api/v1/employees/{f.People["cashier"].Id}/reset-password", new ResetPasswordRequest("NewPassword")),
            _ => await client.PostAsJsonAsync($"/api/v1/employees/{f.People["cashier"].Id}/reset-pin", new ResetPinRequest("654321"))
        };
        using (var anonymous = await Send()) await Expect(anonymous, HttpStatusCode.Unauthorized);
        await f.LoginAsync(client, "owner");
        await using (var db = f.Db()) await db.RolePermissions.Where(r => r.RoleId == f.Owner.Id).ExecuteDeleteAsync();
        using (var forbidden = await Send()) await Expect(forbidden, HttpStatusCode.Forbidden);
    }

    [EmployeePostgresTheory]
    [InlineData("username")]
    [InlineData("pin")]
    public async Task Concurrent_create_retries_snapshot_and_returns_created_plus_conflict(string duplicate)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var first = f.Client();
        using var second = f.Client();
        await f.LoginAsync(first, "owner");
        await f.LoginAsync(second, "owner2");
        f.Barrier.Match = "normalized_username";
        var responses = await Task.WhenAll(
            Create(first, f, username: "ConcurrentUser", pin: "654321"),
            Create(second, f, username: duplicate == "username" ? "concurrentuser" : "OtherUser",
                pin: duplicate == "pin" ? "654321" : "654322"));
        try
        {
            Assert.Equal(new[] { 201, 409 }, responses.Select(r => (int)r.StatusCode).Order().ToArray());
            await using var db = f.Db();
            Assert.Equal(1, await db.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).CountAsync());
            Assert.Equal(1, await db.Employees.CountAsync(e => e.Username == "ConcurrentUser" || e.Username == "concurrentuser" || e.Username == "OtherUser"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Concurrent_transfer_of_equal_pins_retries_once_and_returns_ok_plus_conflict(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        var destination = new Store("Concurrent destination");
        await using (var db = f.Db())
        {
            db.Stores.Add(destination);
            var other = await db.Employees.SingleAsync(e => e.Id == f.People["cashierB"].Id);
            other.ResetCredentialPin(f.People["cashier"].PinHash, f.People["cashier"].PinLookupHash!);
            await db.SaveChangesAsync();
        }
        using var first = f.Client();
        using var second = f.Client();
        await f.LoginAsync(first, "chain");
        await f.LoginAsync(second, "chain2");
        f.Barrier.Match = "pin_lookup_hash";
        var responses = await Task.WhenAll(
            Update(first, f.People["cashier"], store: destination.Id),
            Update(second, f.People["cashierB"], store: destination.Id));
        try
        {
            Assert.Equal(new[] { 200, 409 }, responses.Select(r => (int)r.StatusCode).Order().ToArray());
            await using var db = f.Db();
            Assert.Equal(1, await db.Employees.CountAsync(e => e.StoreId == destination.Id));
            Assert.Single(await db.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [EmployeePostgresTheory]
    [InlineData("role")]
    [InlineData("store")]
    [InlineData("chain")]
    [InlineData("active")]
    [InlineData("name")]
    public async Task Permission_cache_is_invalidated_only_for_actual_changes_after_commit(string field)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "chain");
        var target = f.People["cashier"];
        var key = $"perm:{target.Id}";
        await f.Cache.SetAsync(key, new[] { "employees:read" });
        f.Cache.OnRemove = async removed =>
        {
            Assert.Equal(key, removed);
            await using var db = f.Db();
            Assert.Single(await db.AuditLogs.Where(a => a.EntityId == target.Id).ToListAsync());
            var saved = await db.Employees.SingleAsync(e => e.Id == target.Id);
            if (field == "role") Assert.Equal(f.Manager.Id, saved.RoleId);
            if (field == "store") Assert.Equal(f.B.Id, saved.StoreId);
            if (field == "chain") Assert.True(saved.IsChainOwner);
            if (field == "active") Assert.False(saved.IsActive);
        };
        using var response = field switch
        {
            "role" => await Update(client, target, role: f.Manager.Id),
            "store" => await Update(client, target, store: f.B.Id),
            "chain" => await Update(client, target, chain: true),
            "active" => await client.PutAsJsonAsync($"/api/v1/employees/{target.Id}/lock", new LockEmployeeRequest(false)),
            _ => await Update(client, target)
        };
        await Expect(response, HttpStatusCode.OK);
        Assert.Equal(field == "name" ? 0 : 1, f.Cache.Removed.Count);
    }

    [EmployeePostgresTheory]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("shift")]
    [InlineData("missing-own")]
    [InlineData("missing-destination")]
    [InlineData("conflict")]
    [InlineData("inactive-destination")]
    public async Task Transfer_reuses_T19_guards(string guard)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        var target = f.People["cashier"];
        await using (var db = f.Db())
        {
            var saved = await db.Employees.SingleAsync(e => e.Id == target.Id);
            switch (guard)
            {
                case "inactive": saved.SetActive(false); break;
                case "locked": for (var i = 0; i < 5; i++) saved.RegisterFailedLogin(DateTime.UtcNow); break;
                case "shift": db.Shifts.Add(Shift.Open(f.A.Id, target.Id, 0, null)); break;
                case "missing-own": db.Entry(saved).Property(e => e.PinLookupHash).CurrentValue = null; break;
                case "missing-destination":
                    var other = await db.Employees.SingleAsync(e => e.Id == f.People["cashierB"].Id);
                    db.Entry(other).Property(e => e.PinLookupHash).CurrentValue = null;
                    break;
                case "conflict":
                    var conflict = await db.Employees.SingleAsync(e => e.Id == f.People["cashierB"].Id);
                    conflict.ResetCredentialPin(saved.PinHash, saved.PinLookupHash!);
                    break;
            }
            await db.SaveChangesAsync();
        }
        using var client = f.Client();
        await f.LoginAsync(client, "chain");
        using var response = await Update(client, target, store: guard == "inactive-destination" ? f.Inactive.Id : f.B.Id);
        await Expect(response, guard == "conflict" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest);
        await using var verify = f.Db();
        Assert.Equal(f.A.Id, (await verify.Employees.SingleAsync(e => e.Id == target.Id)).StoreId);
        Assert.Empty(await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
        Assert.Empty(f.Cache.Removed);
    }

    [EmployeePostgresTheory]
    [InlineData("update")]
    [InlineData("lock")]
    [InlineData("password")]
    [InlineData("pin")]
    public async Task Inactive_primary_store_rejects_all_mutations(string action)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "chain");
        await using (var db = f.Db())
        {
            var store = await db.Stores.SingleAsync(s => s.Id == f.B.Id);
            store.UpdateStatus(false);
            await db.SaveChangesAsync();
        }
        var target = f.People["cashierB"];
        using var response = action switch
        {
            "update" => await Update(client, target),
            "lock" => await client.PutAsJsonAsync($"/api/v1/employees/{target.Id}/lock", new LockEmployeeRequest(false)),
            "password" => await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-password", new ResetPasswordRequest("NewPassword")),
            _ => await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-pin", new ResetPinRequest("654321"))
        };
        await Expect(response, HttpStatusCode.BadRequest);
        await using var verify = f.Db();
        Assert.Empty(await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
    }

    [EmployeePostgresTheory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("١٢٣٤٥٦")]
    [InlineData("123456\n")]
    [InlineData(" 123456")]
    public async Task Create_and_reset_PIN_reject_non_six_ASCII_digits(string pin)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "owner");
        using var create = await Create(client, f, pin: pin);
        using var reset = await client.PostAsJsonAsync($"/api/v1/employees/{f.People["cashier"].Id}/reset-pin", new ResetPinRequest(pin));
        await Expect(create, HttpStatusCode.BadRequest);
        await Expect(reset, HttpStatusCode.BadRequest);
    }

    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Lock_self_forbidden_unlock_clears_lockout_and_inactive_employee_cannot_login(bool _)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        await f.LoginAsync(client, "chain");
        using var self = await client.PutAsJsonAsync($"/api/v1/employees/{f.People["chain"].Id}/lock", new LockEmployeeRequest(false));
        await Expect(self, HttpStatusCode.Forbidden);
        var target = f.People["cashier"];
        using var locked = await client.PutAsJsonAsync($"/api/v1/employees/{target.Id}/lock", new LockEmployeeRequest(false));
        await Expect(locked, HttpStatusCode.OK);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/employee/login", new LoginRequest(target.Username, EmployeeApiFactory.Password));
        await Expect(login, HttpStatusCode.Unauthorized);
        await using (var db = f.Db())
        {
            var saved = await db.Employees.SingleAsync(e => e.Id == target.Id);
            for (var i = 0; i < 5; i++) saved.RegisterFailedLogin(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }
        using var unlocked = await client.PutAsJsonAsync($"/api/v1/employees/{target.Id}/lock", new LockEmployeeRequest(true));
        await Expect(unlocked, HttpStatusCode.OK);
        var detail = (await unlocked.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!;
        Assert.Equal(0, detail.FailedLoginCount);
        Assert.Null(detail.LockedUntil);
        await using var verify = f.Db();
        Assert.Equal(new[] { "Employee.Locked", "Employee.Unlocked" },
            await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).OrderBy(a => a.CreatedAt).Select(a => a.Action).ToArrayAsync());
    }
}
