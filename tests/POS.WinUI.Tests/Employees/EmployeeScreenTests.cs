using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Employees;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Employees;

namespace POS.WinUI.Tests.Employees;

public sealed class EmployeeScreenTests
{
    private static readonly Guid Store = Guid.NewGuid(), Cashier = Guid.NewGuid(), Manager = Guid.NewGuid();
    private static EmployeeOptionsResponse Options => new([new(Store, "Store", true)],
        [new(Cashier, "Cashier", null, 1, true, ["orders:read"]), new(Manager, "StoreManager", null, 2, true, [])],
        [new(true, "Active"), new(false, "Locked")]);

    [Theory]
    [InlineData("123456", true)]
    [InlineData("012345", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("123456\n", false)]
    [InlineData("12a456", false)]
    [InlineData("１２３４５６", false)]
    public void Create_validates_exactly_six_ascii_pin_digits(string pin, bool valid)
    {
        var form = new EmployeeEditViewModel(Options, true, 3, Store)
            { Name = "Employee", Username = "employee", Password = "password", Pin = pin };
        Assert.Equal(valid, form.Validate());
        Assert.Equal(pin, form.Pin);
    }

    [Fact]
    public void Store_manager_assignment_is_fixed_to_cashier_and_current_store()
    {
        var form = new EmployeeEditViewModel(Options, false, 2, Store);
        Assert.False(form.CanChooseAssignment);
        Assert.False(form.CanChooseChainOwner);
        Assert.Equal(Cashier, form.RoleId);
        Assert.Equal(Store, form.StoreId);
        Assert.Equal(Cashier, Assert.Single(form.Roles).Id);
    }

    [Fact]
    public void Edit_does_not_require_or_fetch_old_credentials_and_tracks_unsaved_changes()
    {
        var detail = new EmployeeDetailResponse(Guid.NewGuid(), "Employee", "employee", Cashier, "Cashier",
            Store, "Store", false, true, 0, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "0901234567");
        var form = new EmployeeEditViewModel(Options, true, 3, Store, detail);
        Assert.False(form.IsDirty);
        Assert.True(form.Validate());
        Assert.Empty(form.Pin);
        Assert.Empty(form.Password);
        form.Phone = "0911111111";
        Assert.True(form.IsDirty);
        Assert.Equal("0911111111", form.ToUpdate().Phone);
    }

    [Fact]
    public async Task Client_encodes_unicode_search_combines_filters_and_attaches_session_headers()
    {
        Uri? uri = null;
        using var http = new HttpClient(new Handler(request =>
        {
            uri = request.RequestUri;
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            Assert.Equal(Store.ToString(), Assert.Single(request.Headers.GetValues("X-Store-Id")));
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(ApiResponse<PagedResponse<EmployeeResponse>>.Ok(new([], 2, 5, 0))) };
        })) { BaseAddress = new("http://localhost/") };
        var session = new SessionService();
        session.SetSession("test-token", "refresh", Guid.NewGuid().ToString(), "Owner", "Owner", Store.ToString(), isChainOwner: true);
        var api = new EmployeeApiClient(http, session);
        await api.GetAllAsync(new(Store, Cashier, "Hà & 090", false, 2, 5));
        Assert.Contains("search=H%C3%A0%20%26%20090", uri!.AbsoluteUri);
        Assert.Contains("isActive=false", uri.Query);
        Assert.Contains($"roleId={Cashier}", uri.Query);
        Assert.Contains("pageNumber=2&pageSize=5", uri.Query);
    }

    [Fact]
    public async Task Api_errors_remain_visible_to_the_employee_screen()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.Conflict)
        { Content = new StringContent("{\"error\":{\"message\":\"PIN already used\"}}") })) { BaseAddress = new("http://localhost/") };
        var api = new EmployeeApiClient(http, new SessionService());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => api.ResetPinAsync(Guid.NewGuid(), new("123456")));
        Assert.Equal("PIN already used", error.Message);
    }

    [Fact]
    public async Task Refresh_preserves_all_filters_and_handles_an_empty_result()
    {
        using var http = new HttpClient(new Handler(request => new(HttpStatusCode.OK)
        {
            Content = request.RequestUri!.AbsolutePath.EndsWith("/options")
                ? JsonContent.Create(ApiResponse<EmployeeOptionsResponse>.Ok(Options))
                : JsonContent.Create(ApiResponse<PagedResponse<EmployeeResponse>>.Ok(new([], 1, 5, 0)))
        })) { BaseAddress = new("http://localhost/") };
        var session = new SessionService();
        session.SetSession("test-token", "refresh", Guid.NewGuid().ToString(), "Owner", "Owner", Store.ToString(), isChainOwner: true);
        using var vm = new EmployeesViewModel(new EmployeeApiClient(http, session), session, new Dialogs());
        await vm.InitializeAsync();
        vm.BranchId = Store;
        vm.RoleId = Cashier;
        vm.ActiveStatus = false;
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(Store, vm.BranchId);
        Assert.Equal(Cashier, vm.RoleId);
        Assert.False(vm.ActiveStatus);
        Assert.True(vm.IsEmpty);
        Assert.Null(vm.ListError);
        Assert.False(vm.IsLoading);
    }

    private sealed class Dialogs : IConfirmDialogService
    {
        public Task<bool> ShowConfirmAsync(ConfirmDialogOptions options) => Task.FromResult(true);
        public Task<bool> ShowDangerConfirmAsync(string title, string message, string? warningMessage = null, string confirmText = "") => Task.FromResult(true);
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
