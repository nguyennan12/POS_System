namespace POS.Application.Abstractions.Persistence;

public static class PersistenceConstraints
{
    public const string EmployeeUsernameUnique = "IX_employees_username";
    public const string EmployeeNormalizedUsernameUnique = "IX_employees_normalized_username";
    public const string EmployeeStorePinLookupUnique = "IX_employees_store_id_pin_lookup_hash";
    public const string RoleStoreNameUnique = "IX_roles_store_id_name";
    public const string RoleSystemNameUnique = "IX_roles_name";
}
