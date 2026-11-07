using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;

namespace POS.Api.Tests.Employees;

public sealed class EmployeeScreenIntegrationTests
{
    [EmployeePostgresTheory]
    [InlineData(true)]
    public async Task Profile_round_trip_search_and_combined_filters_use_persisted_data(bool _)
    {
        await using var factory = new EmployeeApiFactory();
        await factory.InitializeAsync();
        using var client = factory.Client();
        await factory.LoginAsync(client, "chain");
        var request = new CreateEmployeeRequest("Nguyễn T21", "employee.t21", EmployeeApiFactory.Password,
            "912345", factory.Cashier.Id, factory.A.Id, Phone: "0909999999", Email: "t21@example.com",
            CitizenId: "123456789012", HireDate: new DateOnly(2026, 1, 1), EmploymentType: "Full-time", HourlyWage: 65000m);
        using var created = await client.PostAsJsonAsync("/api/v1/employees", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var employee = (await created.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!;
        Assert.Equal(request.Phone, employee.Phone);
        Assert.Equal(request.HireDate, employee.HireDate);
        using var update = await client.PutAsJsonAsync($"/api/v1/employees/{employee.Id}",
            new UpdateEmployeeRequest("Updated T21", factory.Cashier.Id, factory.B.Id, Phone: "0918888888",
                Email: "updated@example.com", HourlyWage: 70000m));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        await using (var db = factory.Db())
        {
            var stored = await db.Employees.SingleAsync(e => e.Id == employee.Id);
            Assert.Equal("0918888888", stored.Phone);
            Assert.Equal(70000m, stored.HourlyWage);
            Assert.Equal(factory.B.Id, stored.StoreId);
        }
        var page = (await client.GetFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>(
            $"/api/v1/employees?search=091888&storeId={factory.B.Id}&roleId={factory.Cashier.Id}&isActive=true&pageSize=5"))!.Data!;
        Assert.Equal(employee.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
        var empty = (await client.GetFromJsonAsync<ApiResponse<PagedResponse<EmployeeResponse>>>(
            $"/api/v1/employees?search=091888&storeId={factory.A.Id}"))!.Data!;
        Assert.Empty(empty.Items);
    }

    [EmployeePostgresTheory]
    [InlineData("chain", 3)]
    [InlineData("manager", 1)]
    public async Task Options_are_authenticated_and_scoped_without_roles_manage_permission(string caller, int storeCount)
    {
        await using var factory = new EmployeeApiFactory();
        await factory.InitializeAsync();
        using var client = factory.Client();
        using var anonymous = await client.GetAsync("/api/v1/employees/options");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await factory.LoginAsync(client, caller);
        var options = (await client.GetFromJsonAsync<ApiResponse<EmployeeOptionsResponse>>("/api/v1/employees/options"))!.Data!;
        Assert.Equal(storeCount, options.Stores.Count);
        Assert.Contains(options.Roles, r => r.Id == factory.Cashier.Id);
        Assert.Equal(new[] { true, false }, options.Statuses.Select(s => s.IsActive));
        if (caller == "manager") Assert.All(options.Stores, s => Assert.Equal(factory.A.Id, s.Id));
    }

    [EmployeePostgresTheory]
    [InlineData("12a456", "0901234567")]
    [InlineData("12345", "0901234567")]
    [InlineData("912345", "invalid")]
    public async Task Invalid_pin_or_phone_is_rejected_without_inserting(string pin, string phone)
    {
        await using var factory = new EmployeeApiFactory();
        await factory.InitializeAsync();
        using var client = factory.Client();
        await factory.LoginAsync(client, "chain");
        using var response = await client.PostAsJsonAsync("/api/v1/employees", new CreateEmployeeRequest(
            "Invalid", "invalid.t21", EmployeeApiFactory.Password, pin, factory.Cashier.Id, factory.A.Id, Phone: phone));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = factory.Db();
        Assert.False(await db.Employees.AnyAsync(e => e.Username == "invalid.t21"));
    }
}
