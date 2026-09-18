using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using POS.Domain.Auditing;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Api.Tests.Employees;

public class EmployeeRulesIntegrationTests
{
    public static IEnumerable<object[]> Rules => Enumerable.Range(1, 22)
        .SelectMany(rule => new[] { new object[] { rule, true }, new object[] { rule, false } });

    [EmployeePostgresTheory]
    [MemberData(nameof(Rules))]
    public async Task Business_rule_positive_and_negative_through_real_JWT(int rule, bool allowed)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        var target = f.People["cashier"];
        var expected = allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        HttpResponseMessage response;
        switch (rule)
        {
            case 1: // Only a chain owner can create another chain owner.
                await f.LoginAsync(client, allowed ? "chain" : "owner");
                response = await Create(client, f, chain: true);
                if (allowed) expected = HttpStatusCode.Created;
                break;
            case 2: // Manager can create only a Cashier in their own store.
                await f.LoginAsync(client, "manager");
                response = await Create(client, f, role: allowed ? f.Cashier.Id : f.Manager.Id);
                if (allowed) expected = HttpStatusCode.Created;
                break;
            case 3: // Current target level must be lower, including profile edits.
                await f.LoginAsync(client, "owner");
                target = f.People[allowed ? "manager" : "owner2"];
                response = await Update(client, target);
                break;
            case 4: // Only a chain owner may toggle the flag.
                await f.LoginAsync(client, allowed ? "chain" : "owner");
                response = await Update(client, target, chain: true);
                break;
            case 5: // Last active owner protection.
                await f.LoginAsync(client, "chain");
                if (allowed) target = f.People["owner"];
                else
                {
                    await using var db = f.Db();
                    var store = new Store("Only owner store");
                    target = new Employee("last", "last", "unused", "unused", f.Owner.Id, storeId: store.Id);
                    db.Stores.Add(store);
                    db.Employees.Add(target);
                    await db.SaveChangesAsync();
                }
                response = await client.PutAsJsonAsync($"/api/v1/employees/{target.Id}/lock", new LockEmployeeRequest(false));
                break;
            case 6: // Cashier sees self in detail AND list.
                await f.LoginAsync(client, "cashier");
                using (var list = await client.GetAsync("/api/v1/employees"))
                {
                    await Expect(list, HttpStatusCode.OK);
                    var page = (await list.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>())!.Data!;
                    Assert.Equal(target.Id, Assert.Single(page.Items).Id);
                }
                response = await client.GetAsync($"/api/v1/employees/{f.People[allowed ? "cashier" : "manager"].Id}");
                break;
            case 7: // A store-less chain owner is manageable only by another chain owner.
                await f.LoginAsync(client, allowed ? "chain" : "owner");
                target = f.People["chainNull"];
                response = await Update(client, target);
                break;
            case 8: // Transfer allowed only for current Cashier/StoreManager.
                await f.LoginAsync(client, "chain");
                target = f.People[allowed ? "cashier" : "owner"];
                response = await Update(client, target, store: f.B.Id);
                break;
            case 9: // Demotion deletes access rows in the same transaction.
                target = f.People["chain2"];
                await using (var db = f.Db())
                {
                    db.EmployeeStoreAccesses.Add(new EmployeeStoreAccess(target.Id, f.B.Id, f.People["chain"].Id));
                    await db.SaveChangesAsync();
                }
                await f.LoginAsync(client, allowed ? "chain" : "owner");
                response = await Update(client, target, chain: false);
                await using (var verify = f.Db())
                    Assert.Equal(!allowed, await verify.EmployeeStoreAccesses.AnyAsync(a => a.EmployeeId == target.Id));
                break;
            case 10: // Store must be active.
                await f.LoginAsync(client, "chain");
                response = await Create(client, f, store: allowed ? f.A.Id : f.Inactive.Id);
                expected = allowed ? HttpStatusCode.Created : HttpStatusCode.BadRequest;
                break;
            case 11: // Trim display username; uniqueness is case-insensitive.
                await f.LoginAsync(client, "owner");
                response = await Create(client, f, username: allowed ? "  MixedCase  " : "  CASHIER  ");
                expected = allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict;
                if (allowed)
                    Assert.Equal("MixedCase", (await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!.Username);
                break;
            case 12: // PIN conflicts include inactive and locked accounts.
                await using (var db = f.Db())
                {
                    var existing = await db.Employees.SingleAsync(e => e.Id == target.Id);
                    existing.SetActive(false);
                    for (var i = 0; i < 5; i++) existing.RegisterFailedLogin(DateTime.UtcNow);
                    await db.SaveChangesAsync();
                }
                await f.LoginAsync(client, "chain");
                response = await Create(client, f, pin: "100006", store: allowed ? f.B.Id : f.A.Id);
                expected = allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict;
                break;
            case 13: // Chain owner PIN belongs only to the primary store, not access rows.
                await using (var db = f.Db())
                {
                    db.EmployeeStoreAccesses.Add(new EmployeeStoreAccess(f.People["chain2"].Id, f.B.Id, f.People["chain"].Id));
                    await db.SaveChangesAsync();
                }
                await f.LoginAsync(client, "chain");
                response = await Create(client, f, pin: "100002", store: allowed ? f.B.Id : f.A.Id);
                expected = allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict;
                break;
            case 14:
            case 15:
            case 16:
                await VerifyCredentialReset(f, client, rule != 14, allowed);
                return;
            case 17: // No old PIN in the request; new PIN is still required.
                await f.LoginAsync(client, "owner");
                response = await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-pin",
                    new ResetPinRequest(allowed ? "654321" : ""));
                expected = allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                break;
            case 18: // ASCII digits only.
                await f.LoginAsync(client, "owner");
                response = await Create(client, f, pin: allowed ? "654321" : "１２３４５６");
                expected = allowed ? HttpStatusCode.Created : HttpStatusCode.BadRequest;
                break;
            case 19: // Stable paging and history defaults.
                await f.LoginAsync(client, "owner");
                if (allowed)
                {
                    using var list = await client.GetAsync("/api/v1/employees");
                    await Expect(list, HttpStatusCode.OK);
                    var page = (await list.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>())!.Data!;
                    await using var db = f.Db();
                    var ids = await db.Employees.Where(e => e.StoreId == f.A.Id).OrderBy(e => e.Name).ThenBy(e => e.Id).Select(e => e.Id).ToListAsync();
                    Assert.Equal(ids, page.Items.Select(e => e.Id));
                    db.AuditLogs.AddRange(new AuditLog(target.Id, target.Id, f.A.Id, "Login"), new AuditLog(target.Id, target.Id, f.A.Id, "Logout"));
                    await db.SaveChangesAsync();
                }
                response = await client.GetAsync($"/api/v1/employees/{target.Id}/login-history" + (allowed ? "" : "?PageNumber=0"));
                expected = allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                if (allowed)
                {
                    var page = (await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LoginHistoryResponse>>>())!.Data!;
                    Assert.Equal(1, page.PageNumber);
                    Assert.Equal(20, page.PageSize);
                    Assert.Equal(2, page.TotalCount);
                    Assert.True(page.Items[0].CreatedAt >= page.Items[1].CreatedAt);
                }
                break;
            case 20: // Mutation audit identifies actor and target, without credentials.
                await f.LoginAsync(client, allowed ? "owner" : "custom");
                response = await Create(client, f);
                if (allowed) expected = HttpStatusCode.Created;
                await using (var db = f.Db())
                {
                    var audits = await db.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync();
                    if (allowed)
                    {
                        var entry = Assert.Single(audits);
                        var created = (await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!;
                        Assert.Equal(f.People["owner"].Id, entry.EmployeeId);
                        Assert.Equal(created.Id, entry.EntityId);
                        Assert.Equal("Employee", entry.EntityType);
                        Assert.Equal("Employee.Created", entry.Action);
                        Assert.Null(entry.Description);
                        var saved = await db.Employees.SingleAsync(e => e.Id == created.Id);
                        Assert.True(BCrypt.Net.BCrypt.Verify(EmployeeApiFactory.Password, saved.PasswordHash));
                        Assert.True(BCrypt.Net.BCrypt.Verify("654321", saved.PinHash));
                        Assert.DoesNotContain(saved.PasswordHash, await response.Content.ReadAsStringAsync());
                        Assert.DoesNotContain(saved.PinHash, await response.Content.ReadAsStringAsync());
                    }
                    else Assert.Empty(audits);
                }
                break;
            case 21: // Missing lookup on target or other employees does not prevent reset.
                await using (var db = f.Db())
                {
                    await db.Employees.Where(e => e.Id == target.Id || e.Id == f.People["manager"].Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.PinLookupHash, (string?)null));
                }
                await f.LoginAsync(client, "owner");
                response = await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-pin",
                    new ResetPinRequest(allowed ? "654321" : "100002"));
                expected = allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict;
                break;
            case 22: // Custom role named Owner still has level 0.
                await f.LoginAsync(client, "custom");
                response = allowed ? await client.GetAsync($"/api/v1/employees/{f.People["custom"].Id}") :
                    await Update(client, target, role: f.Owner.Id);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(rule));
        }
        using (response) await Expect(response, expected);
    }

    internal static Task<HttpResponseMessage> Create(HttpClient client, EmployeeApiFactory f,
        Guid? role = null, Guid? store = null, bool chain = false, string username = "newEmployee", string pin = "654321") =>
        client.PostAsJsonAsync("/api/v1/employees", new CreateEmployeeRequest("New employee", username,
            EmployeeApiFactory.Password, pin, role ?? f.Cashier.Id, store ?? f.A.Id, chain));

    internal static Task<HttpResponseMessage> Update(HttpClient client, Employee target,
        Guid? role = null, Guid? store = null, bool? chain = null) =>
        client.PutAsJsonAsync($"/api/v1/employees/{target.Id}", new UpdateEmployeeRequest("Updated",
            role ?? target.RoleId, store ?? target.StoreId, chain ?? target.IsChainOwner));

    internal static async Task Expect(HttpResponseMessage response, HttpStatusCode expected) =>
        Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

    private static async Task VerifyCredentialReset(EmployeeApiFactory f, HttpClient client, bool pin, bool allowed)
    {
        var first = await f.LoginAsync(client, "cashier");
        var second = await f.LoginAsync(client, "cashier");
        var target = f.People["cashier"];
        await using (var db = f.Db())
        {
            var employee = await db.Employees.SingleAsync(e => e.Id == target.Id);
            for (var i = 0; i < 5; i++) employee.RegisterFailedLogin(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }
        await f.LoginAsync(client, allowed ? "owner" : "managerB");
        using var reset = pin
            ? await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-pin", new ResetPinRequest("654321"))
            : await client.PostAsJsonAsync($"/api/v1/employees/{target.Id}/reset-password", new ResetPasswordRequest("New-password!"));
        await Expect(reset, allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        await using var verify = f.Db();
        var saved = await verify.Employees.SingleAsync(e => e.Id == target.Id);
        var tokens = await verify.RefreshTokens.Where(t => t.EmployeeId == target.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        if (!allowed)
        {
            Assert.Equal(5, saved.FailedLoginCount);
            Assert.NotNull(saved.LockedUntil);
            Assert.All(tokens, t => Assert.Null(t.RevokedAt));
            Assert.Empty(await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).ToListAsync());
            return;
        }
        Assert.Equal(0, saved.FailedLoginCount);
        Assert.Null(saved.LockedUntil);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        Assert.True(BCrypt.Net.BCrypt.Verify(pin ? "654321" : "New-password!", pin ? saved.PinHash : saved.PasswordHash));
        Assert.Equal(pin ? "Employee.PinReset" : "Employee.PasswordReset", (await verify.AuditLogs.Where(a => a.Action.StartsWith("Employee.")).SingleAsync()).Action);
        foreach (var old in new[] { first.RefreshToken, second.RefreshToken })
        {
            using var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(old));
            await Expect(refresh, HttpStatusCode.Unauthorized);
        }
        using var login = await client.PostAsJsonAsync("/api/v1/auth/employee/login",
            new LoginRequest(target.Username, pin ? EmployeeApiFactory.Password : "New-password!"));
        await Expect(login, HttpStatusCode.OK);
    }
}
