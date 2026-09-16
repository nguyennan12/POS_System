using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using static POS.Api.Tests.Employees.EmployeeRulesIntegrationTests;

namespace POS.Api.Tests.Employees;

public class EmployeeLoginHistoryIntegrationTests
{
    [EmployeePostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_auth_writes_login_failed_login_logout_and_history_excludes_profile_audit(bool pin)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        client.DefaultRequestHeaders.Add("X-Device-Id", "employee-history-tests");
        var target = f.People["cashier"];
        if (pin)
        {
            // Simulate a known employee with a mismatched credential hash: lookup succeeds, BCrypt fails.
            await using var db = f.Db();
            var employee = await db.Employees.SingleAsync(e => e.Id == target.Id);
            employee.ResetCredentialPin(BCrypt.Net.BCrypt.HashPassword("000000"), target.PinLookupHash!);
            await db.SaveChangesAsync();
        }
        using var failed = pin
            ? await client.PostAsJsonAsync("/api/v1/auth/employee/pin", new PinLoginRequest("100006", f.A.Id))
            : await client.PostAsJsonAsync("/api/v1/auth/employee/login", new LoginRequest(target.Username, "wrong-password"));
        await Expect(failed, HttpStatusCode.Unauthorized);
        if (pin)
        {
            await using var db = f.Db();
            var employee = await db.Employees.SingleAsync(e => e.Id == target.Id);
            employee.ResetCredentialPin(target.PinHash, target.PinLookupHash!);
            await db.SaveChangesAsync();
        }
        using var success = pin
            ? await client.PostAsJsonAsync("/api/v1/auth/employee/pin", new PinLoginRequest("100006", f.A.Id))
            : await client.PostAsJsonAsync("/api/v1/auth/employee/login", new LoginRequest(target.Username, EmployeeApiFactory.Password));
        await Expect(success, HttpStatusCode.OK);
        var auth = (await success.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(auth.RefreshToken));
        await Expect(logout, HttpStatusCode.NoContent);
        using var repeatedLogout = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(auth.RefreshToken));
        await Expect(repeatedLogout, HttpStatusCode.NoContent);
        await f.LoginAsync(client, "owner");
        using var update = await Update(client, target);
        await Expect(update, HttpStatusCode.OK);
        using var history = await client.GetAsync($"/api/v1/employees/{target.Id}/login-history");
        await Expect(history, HttpStatusCode.OK);
        var page = (await history.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LoginHistoryResponse>>>())!.Data!;
        Assert.Equal(new[] { "Logout", "Login", "LoginFailed" }, page.Items.Select(a => a.Action));
        Assert.All(page.Items, a => Assert.Null(a.Description));
        var raw = await history.Content.ReadAsStringAsync();
        Assert.DoesNotContain("wrong-password", raw);
        Assert.DoesNotContain(EmployeeApiFactory.Password, raw);
        Assert.DoesNotContain("100006", raw);
        Assert.DoesNotContain(auth.RefreshToken, raw);
        Assert.DoesNotContain(auth.AccessToken, raw);
    }

    [EmployeePostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_login_is_audited_without_credentials_or_a_fabricated_employee(bool pin)
    {
        await using var f = new EmployeeApiFactory();
        await f.InitializeAsync();
        using var client = f.Client();
        client.DefaultRequestHeaders.Add("X-Device-Id", "employee-history-tests");
        using var failed = pin
            ? await client.PostAsJsonAsync("/api/v1/auth/employee/pin", new PinLoginRequest("999999", f.A.Id))
            : await client.PostAsJsonAsync("/api/v1/auth/employee/login", new LoginRequest("unknown-account", "secret"));
        await Expect(failed, HttpStatusCode.Unauthorized);
        await using var db = f.Db();
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("LoginFailed", audit.Action);
        Assert.Null(audit.EmployeeId);
        Assert.Equal(Guid.Empty, audit.EntityId);
        Assert.Null(audit.Description);
    }
}
