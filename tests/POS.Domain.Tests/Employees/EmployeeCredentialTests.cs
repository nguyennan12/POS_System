using POS.Domain.Employees;

namespace POS.Domain.Tests.Employees;

public class EmployeeCredentialTests
{
    private static Employee LockedEmployee()
    {
        var employee = new Employee("Employee", "User", "old-password", "old-pin", Guid.NewGuid(), storeId: Guid.NewGuid());
        employee.setPinLookUpHash("old-lookup");
        for (var i = 0; i < 5; i++) employee.RegisterFailedLogin(DateTime.UtcNow);
        return employee;
    }

    [Fact]
    public void Password_reset_clears_lockout_without_changing_PIN()
    {
        var employee = LockedEmployee();
        employee.ResetCredentialPassword("new-password");
        Assert.Equal("new-password", employee.PasswordHash);
        Assert.Equal("old-pin", employee.PinHash);
        Assert.Equal("old-lookup", employee.PinLookupHash);
        Assert.Equal(0, employee.FailedLoginCount);
        Assert.Null(employee.LockedUntil);
    }

    [Fact]
    public void PIN_reset_replaces_both_hashes_and_clears_lockout_without_changing_password()
    {
        var employee = LockedEmployee();
        employee.ResetCredentialPin("new-pin", "new-lookup");
        Assert.Equal("old-password", employee.PasswordHash);
        Assert.Equal("new-pin", employee.PinHash);
        Assert.Equal("new-lookup", employee.PinLookupHash);
        Assert.Equal(0, employee.FailedLoginCount);
        Assert.Null(employee.LockedUntil);
    }

    [Theory]
    [InlineData("", "lookup")]
    [InlineData("hash", "")]
    public void Invalid_PIN_hashes_do_not_partially_update_credentials(string hash, string lookup)
    {
        var employee = LockedEmployee();
        Assert.Throws<ArgumentException>(() => employee.ResetCredentialPin(hash, lookup));
        Assert.Equal("old-pin", employee.PinHash);
        Assert.Equal("old-lookup", employee.PinLookupHash);
        Assert.Equal(5, employee.FailedLoginCount);
    }

    [Fact]
    public void Unlock_resets_lockout_while_lock_preserves_it()
    {
        var employee = LockedEmployee();
        employee.SetActive(false);
        Assert.False(employee.IsActive);
        Assert.NotNull(employee.LockedUntil);
        employee.SetActive(true);
        Assert.True(employee.IsActive);
        Assert.Null(employee.LockedUntil);
        Assert.Equal(0, employee.FailedLoginCount);
    }

    [Fact]
    public void Profile_enforces_store_invariant_and_never_changes_username_or_credentials()
    {
        var employee = LockedEmployee();
        Assert.Throws<ArgumentException>(() => employee.UpdateProfile("Updated", employee.RoleId, null, false));
        employee.UpdateProfile("Updated", employee.RoleId, null, true);
        Assert.Equal("User", employee.Username);
        Assert.Equal("old-password", employee.PasswordHash);
        Assert.Equal("old-pin", employee.PinHash);
        Assert.True(employee.IsChainOwner);
        Assert.Null(employee.StoreId);
    }
}
