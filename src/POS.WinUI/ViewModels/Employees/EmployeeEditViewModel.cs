using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Employees;
using POS.WinUI.Core.Services;

namespace POS.WinUI.ViewModels.Employees;

public record EmployeeChoice<T>(T Value, string Name);

public sealed partial class EmployeeEditViewModel : ObservableObject
{
    private readonly IReadOnlyList<EmployeeRoleOption> _roles;
    private readonly bool _chainCaller;
    private readonly int _callerLevel;
    private string _baseline = "";
    public Guid? Id { get; }
    public bool IsCreate => Id is null;
    public LocalizationService Loc => LocalizationService.Instance;
    public bool CanChooseAssignment => _callerLevel >= 3;
    public bool CanChooseChainOwner => _chainCaller;
    public ObservableCollection<EmployeeChoice<Guid?>> Stores { get; } = [];
    public ObservableCollection<EmployeeRoleOption> Roles { get; } = [];
    public IReadOnlyList<string> EmploymentTypes { get; } = ["Full-time", "Part-time", "Contract"];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _citizenId = "";
    [ObservableProperty] private DateTime? _hireDate;
    [ObservableProperty] private string? _employmentType;
    [ObservableProperty] private string _hourlyWage = "";
    [ObservableProperty] private Guid? _storeId;
    [ObservableProperty] private Guid? _roleId;
    [ObservableProperty] private bool _isChainOwner;
    [ObservableProperty] private string? _errorMessage;
    public string Password { get; set; } = "";
    public string Pin { get; set; } = "";
    public bool IsDirty => Snapshot() != _baseline;
    public string Permissions => string.Join("\n", _roles.FirstOrDefault(r => r.Id == RoleId)?.Permissions ?? []);

    public EmployeeEditViewModel(EmployeeOptionsResponse options, bool chainCaller, int callerLevel,
        Guid? currentStore, EmployeeDetailResponse? employee = null)
    {
        _roles = options.Roles;
        _chainCaller = chainCaller;
        _callerLevel = callerLevel;
        Id = employee?.Id;
        if (chainCaller) Stores.Add(new(null, "All stores (chain owner)"));
        foreach (var store in options.Stores.Where(s => s.IsActive || s.Id == employee?.StoreId))
            Stores.Add(new(store.Id, store.Name));
        _name = employee?.Name ?? "";
        _username = employee?.Username ?? "";
        _phone = employee?.Phone ?? "";
        _email = employee?.Email ?? "";
        _citizenId = employee?.CitizenId ?? "";
        _hireDate = employee?.HireDate?.ToDateTime(TimeOnly.MinValue);
        _employmentType = employee?.EmploymentType;
        _hourlyWage = employee?.HourlyWage?.ToString(CultureInfo.InvariantCulture) ?? "";
        _isChainOwner = employee?.IsChainOwner ?? false;
        _storeId = employee is null ? currentStore : employee.StoreId;
        _roleId = employee?.RoleId;
        UpdateRoles();
        if (employee == null) _roleId = Roles.FirstOrDefault(r => r.Name == "Cashier")?.Id;
        _baseline = Snapshot();
    }

    partial void OnStoreIdChanged(Guid? value) => UpdateRoles();
    partial void OnRoleIdChanged(Guid? value) => OnPropertyChanged(nameof(Permissions));
    private void UpdateRoles()
    {
        var selected = RoleId;
        Roles.Clear();
        foreach (var role in _roles.Where(r => (r.StoreId is null || r.StoreId == StoreId) &&
            (_chainCaller || (_callerLevel == 2 ? r.Level == 1 : r.Level < _callerLevel) || r.Id == selected)))
            Roles.Add(role);
        if (!Roles.Any(r => r.Id == selected)) RoleId = null;
        else RoleId = selected;
        OnPropertyChanged(nameof(Permissions));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is not (nameof(IsDirty) or nameof(Permissions) or nameof(ErrorMessage)))
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsDirty)));
    }

    public bool Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 200) errors.Add("Full name is required (max 200 characters).");
        if (IsCreate && (string.IsNullOrWhiteSpace(Username) || Username.Trim().Length > 50)) errors.Add("Username is required (max 50 characters).");
        if (IsCreate && string.IsNullOrWhiteSpace(Password)) errors.Add("Initial password is required.");
        if (IsCreate && !Regex.IsMatch(Pin, "\\A[0-9]{6}\\z")) errors.Add("PIN must contain exactly 6 digits (0–9).");
        if (RoleId is null) errors.Add("Select a role.");
        if (StoreId is null && !IsChainOwner) errors.Add("Select a branch.");
        if (Phone.Length > 0 && !Regex.IsMatch(Phone.Trim(), @"^\+?[0-9]{9,15}$")) errors.Add("Phone must contain 9–15 digits, optionally prefixed by +.");
        if (Email.Length > 0 && (Email.Trim().Length > 254 || !MailAddress.TryCreate(Email.Trim(), out _))) errors.Add("Enter a valid email address.");
        if (CitizenId.Length > 0 && !Regex.IsMatch(CitizenId.Trim(), "^[0-9]{12}$")) errors.Add("Citizen ID must contain 12 digits.");
        if (HireDate > DateTime.Today) errors.Add("Hire date cannot be in the future.");
        if (!string.IsNullOrWhiteSpace(HourlyWage) && (!decimal.TryParse(HourlyWage, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var wage) || wage < 0 || wage > 9999999999999999m)) errors.Add("Hourly wage must be a valid non-negative amount.");
        ErrorMessage = errors.Count == 0 ? null : string.Join("\n", errors);
        return errors.Count == 0;
    }

    public CreateEmployeeRequest ToCreate() => new(Name.Trim(), Username.Trim(), Password, Pin, RoleId!.Value,
        StoreId, IsChainOwner, Clean(Phone), Clean(Email), Clean(CitizenId), Date(), EmploymentType, Wage());
    public UpdateEmployeeRequest ToUpdate() => new(Name.Trim(), RoleId!.Value, StoreId, IsChainOwner,
        Clean(Phone), Clean(Email), Clean(CitizenId), Date(), EmploymentType, Wage());
    public void ClearSecrets() { Password = ""; Pin = ""; }
    private DateOnly? Date() => HireDate is DateTime date ? DateOnly.FromDateTime(date) : null;
    private decimal? Wage() => string.IsNullOrWhiteSpace(HourlyWage) ? null : decimal.Parse(HourlyWage, CultureInfo.InvariantCulture);
    private static string? Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private string Snapshot() => System.Text.Json.JsonSerializer.Serialize(new { Name, Username, Phone, Email, CitizenId, HireDate, EmploymentType, HourlyWage, StoreId, RoleId, IsChainOwner });
}
