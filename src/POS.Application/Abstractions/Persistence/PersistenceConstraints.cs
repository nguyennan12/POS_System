namespace POS.Application.Abstractions.Persistence;

public static class PersistenceConstraints
{
    public const string EmployeeStoreAccessUnique = "IX_employee_store_access_employee_id_store_id";
    public const string EmployeeStorePinLookupUnique = "IX_employees_store_id_pin_lookup_hash";
}
