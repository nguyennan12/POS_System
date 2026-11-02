using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Employees;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Employees;

public sealed partial class EmployeeListItem(EmployeeResponse data) : ObservableObject
{
    public EmployeeResponse Data => data;
    public string Initials => string.Concat(data.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(s => s[0])).ToUpperInvariant();
    public string Store => data.IsChainOwner ? "All stores" : data.StoreName ?? "—";
    public string Status => data.IsActive ? "Active" : "Locked";
    [ObservableProperty] private bool _isSelected;
}

public sealed partial class EmployeesViewModel : ManagementTabViewModelBase
{
    private readonly EmployeeApiClient _api;
    private readonly IConfirmDialogService _dialogs;
    private EmployeeOptionsResponse? _options;
    private EmployeeDetailResponse? _detail;
    private CancellationTokenSource? _listCts, _detailCts;
    private int _listVersion, _detailVersion;
    private bool _initialized, _suppressFilters, _resetPin;
    public override string TabId => "Employees";
    public override string Title => Loc["employees.title"];
    public override string Subtitle => Loc["employees.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.PersonAccounts24;
    public LocalizationService Loc => LocalizationService.Instance;
    public ObservableCollection<EmployeeListItem> Employees { get; } = [];
    public ObservableCollection<EmployeeChoice<Guid?>> Branches { get; } = [];
    public ObservableCollection<EmployeeChoice<Guid?>> Roles { get; } = [];
    public ObservableCollection<EmployeeChoice<bool?>> Statuses { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [5, 10, 20, 50];
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private Guid? _branchId;
    [ObservableProperty] private Guid? _roleId;
    [ObservableProperty] private bool? _activeStatus;
    [ObservableProperty] private int _pageSize = 5;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string? _listError;
    [ObservableProperty] private string? _detailError;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private EmployeeEditViewModel? _editor;
    [ObservableProperty] private EmployeeEditViewModel? _newEmployee;
    [ObservableProperty] private string? _credentialTitle;
    [ObservableProperty] private string? _credentialError;
    public string NewCredential { get; set; } = "";
    public bool IsEmpty => !IsLoading && ListError is null && Employees.Count == 0;
    public bool HasBranchFilter => BranchId.HasValue;
    public bool HasRoleFilter => RoleId.HasValue;
    public bool HasStatusFilter => ActiveStatus.HasValue;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public string PageSummary => $"{PageNumber} / {TotalPages}";
    public string Showing => $"Showing {(TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1)}\u2013{Math.Min(PageNumber * PageSize, TotalCount)} of {TotalCount}";
    public bool CanCreate => HasPermission("employees:manage") && RoleLevel >= 2 && _options is not null && !IsSaving;
    public bool CanManageSelection => !IsDetailLoading && _detail != null && HasPermission("employees:manage") &&
        _detail.Id.ToString() != SessionService.EmployeeId && (SessionService.IsChainOwner ||
        (!_detail.IsChainOwner && _detail.StoreId?.ToString() == CurrentStoreId &&
        RoleLevel > (_options?.Roles.FirstOrDefault(r => r.Id == _detail.RoleId)?.Level ?? 0)));
    public string SelectedName => _detail?.Name ?? "";
    public string SelectedRole => _detail?.RoleName ?? "";
    public string SelectedUsername => _detail?.Username ?? "";
    public string SelectedStore => _detail?.IsChainOwner == true ? "All stores" : _detail?.StoreName ?? "—";
    public string SelectedStatus => _detail?.IsActive == true ? "Active" : "Locked";
    public string LockAction => _detail?.IsActive == true ? "Lock Account" : "Unlock Account";

    public EmployeesViewModel(EmployeeApiClient api, SessionService session, IConfirmDialogService dialogs) : base(session)
    { _api = api; _dialogs = dialogs; }
    public async Task InitializeAsync() { if (!_initialized) { _initialized = true; await LoadAsync(true); } }
    partial void OnSearchChanged(string value) => FilterChanged(true);
    partial void OnBranchIdChanged(Guid? value) { OnPropertyChanged(nameof(HasBranchFilter)); FilterChanged(); }
    partial void OnRoleIdChanged(Guid? value) { OnPropertyChanged(nameof(HasRoleFilter)); FilterChanged(); }
    partial void OnActiveStatusChanged(bool? value) { OnPropertyChanged(nameof(HasStatusFilter)); FilterChanged(); }
    partial void OnPageSizeChanged(int value) => FilterChanged();
    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate)); OpenNewEmployeeCommand.NotifyCanExecuteChanged();
        SaveProfileCommand.NotifyCanExecuteChanged(); CreateEmployeeCommand.NotifyCanExecuteChanged();
        ToggleLockCommand.NotifyCanExecuteChanged(); SubmitCredentialCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsDetailLoadingChanged(bool value) => NotifySelection();
    partial void OnNewEmployeeChanged(EmployeeEditViewModel? value) => SaveProfileCommand.NotifyCanExecuteChanged();
    partial void OnCredentialTitleChanged(string? value) => SaveProfileCommand.NotifyCanExecuteChanged();
    private void FilterChanged(bool debounce = false)
    { if (!_suppressFilters && _initialized) { PageNumber = 1; _ = LoadAsync(false, debounce); } }
    [RelayCommand(CanExecute = nameof(NotSaving))] private async Task RefreshAsync()
    { if (await ConfirmDiscardAsync()) { Editor = null; await LoadAsync(true); } }

    private async Task LoadAsync(bool loadOptions = false, bool debounce = false, Guid? selectId = null)
    {
        _listCts?.Cancel(); _listCts?.Dispose(); _listCts = new();
        var ct = _listCts.Token; var version = ++_listVersion;
        IsLoading = true; ListError = null; NotifyPaging();
        try
        {
            if (debounce) await Task.Delay(300, ct);
            if (loadOptions || _options is null)
            {
                var options = await _api.GetOptionsAsync(ct);
                if (version != _listVersion) return;
                _options = options;
                var branch = BranchId;
                var role = RoleId;
                var status = ActiveStatus;
                _suppressFilters = true;
                try
                {
                    Branches.Clear(); Roles.Clear(); Statuses.Clear();
                    Branches.Add(new(null, "All Branches"));
                    foreach (var s in _options.Stores) Branches.Add(new(s.Id, s.Name));
                    Roles.Add(new(null, "All Roles"));
                    foreach (var r in _options.Roles) Roles.Add(new(r.Id, r.Name));
                    Statuses.Add(new(null, "All Status"));
                    foreach (var s in _options.Statuses) Statuses.Add(new(s.IsActive, s.Name));
                    BranchId = Branches.Any(b => b.Value == branch) ? branch : null;
                    RoleId = Roles.Any(r => r.Value == role) ? role : null;
                    ActiveStatus = Statuses.Any(s => s.Value == status) ? status : null;
                }
                finally { _suppressFilters = false; }
                OnPropertyChanged(nameof(CanCreate)); OpenNewEmployeeCommand.NotifyCanExecuteChanged();
            }
            var page = await _api.GetAllAsync(new(BranchId, RoleId, Search, ActiveStatus, PageNumber, PageSize), ct);
            if (version != _listVersion) return;
            if (page.TotalPages > 0 && PageNumber > page.TotalPages)
            {
                PageNumber = page.TotalPages;
                page = await _api.GetAllAsync(new(BranchId, RoleId, Search, ActiveStatus, PageNumber, PageSize), ct);
            }
            if (version != _listVersion) return;
            if (page.TotalCount == 0) PageNumber = 1;
            TotalCount = page.TotalCount; Employees.Clear();
            foreach (var e in page.Items) Employees.Add(new(e));
            var selected = Employees.FirstOrDefault(e => e.Data.Id == (selectId ?? _detail?.Id)) ?? Employees.FirstOrDefault();
            if (Editor?.IsDirty != true)
            {
                if (selected != null) await SelectCoreAsync(selected.Data.Id);
                else { _detail = null; Editor = null; NotifySelection(); }
            }
            foreach (var item in Employees) item.IsSelected = item.Data.Id == _detail?.Id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (version == _listVersion) ListError = ex.Message; }
        finally { if (version == _listVersion) { IsLoading = false; NotifyPaging(); } }
    }
    [RelayCommand] private async Task SelectEmployeeAsync(EmployeeListItem? item)
    {
        if (item != null && item.Data.Id != _detail?.Id && !IsSaving && await ConfirmDiscardAsync())
            await SelectCoreAsync(item.Data.Id);
    }
    private async Task SelectCoreAsync(Guid id)
    {
        _detailCts?.Cancel(); _detailCts?.Dispose(); _detailCts = new();
        var ct = _detailCts.Token; var version = ++_detailVersion;
        IsDetailLoading = true; DetailError = null;
        try
        {
            var detail = await _api.GetByIdAsync(id, ct);
            if (version != _detailVersion) return;
            _detail = detail; Editor = MakeEditor(detail); NotifySelection();
            foreach (var item in Employees) item.IsSelected = item.Data.Id == id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (version == _detailVersion) { _detail = null; Editor = null; DetailError = ex.Message; NotifySelection(); } }
        finally { if (version == _detailVersion) IsDetailLoading = false; }
    }
    private EmployeeEditViewModel MakeEditor(EmployeeDetailResponse? detail = null) =>
        new(_options!, SessionService.IsChainOwner, RoleLevel, Guid.TryParse(CurrentStoreId, out var store) ? store : null, detail);
    [RelayCommand(CanExecute = nameof(CanCreate))] private async Task OpenNewEmployeeAsync()
    { if (await ConfirmDiscardAsync()) NewEmployee = MakeEditor(); }
    [RelayCommand] private void CloseNewEmployee()
    { if (!IsSaving) { NewEmployee?.ClearSecrets(); NewEmployee = null; } }
    private bool CanSave() => !IsSaving && NewEmployee is null && CredentialTitle is null && CanManageSelection;
    [RelayCommand(CanExecute = nameof(CanSave))] private async Task SaveProfileAsync()
    {
        if (Editor is null || !Editor.Validate() || _detail is null) return;
        IsSaving = true;
        try
        {
            _detail = await _api.UpdateAsync(_detail.Id, Editor.ToUpdate());
            Editor = MakeEditor(_detail); NotifySelection(); ShowSuccess("Profile saved");
            await LoadAsync(selectId: _detail.Id);
        }
        catch (Exception ex) { if (Editor != null) Editor.ErrorMessage = ex.Message; }
        finally { IsSaving = false; }
    }
    private bool NotSaving() => !IsSaving;
    [RelayCommand(CanExecute = nameof(NotSaving))] private async Task CreateEmployeeAsync()
    {
        if (NewEmployee is null || !NewEmployee.Validate()) return;
        IsSaving = true;
        try
        {
            var created = await _api.CreateAsync(NewEmployee.ToCreate());
            NewEmployee.ClearSecrets(); NewEmployee = null; Editor = null; _detail = created;
            _suppressFilters = true; Search = ""; BranchId = null; RoleId = null; ActiveStatus = null; PageNumber = 1; _suppressFilters = false;
            await LoadAsync(selectId: created.Id);
            if (_detail?.Id != created.Id) await SelectCoreAsync(created.Id);
            ShowSuccess("Employee created");
        }
        catch (Exception ex) { if (NewEmployee != null) NewEmployee.ErrorMessage = ex.Message; }
        finally { IsSaving = false; }
    }
    [RelayCommand] private void CancelProfile() { if (!IsSaving && _detail != null) Editor = MakeEditor(_detail); }
    [RelayCommand(CanExecute = nameof(CanSave))] private async Task ToggleLockAsync()
    {
        if (_detail == null || !await ConfirmDiscardAsync()) return;
        if (!await _dialogs.ShowConfirmAsync(new() { Title = LockAction, Message = $"{LockAction}: {_detail.Name}?", ConfirmText = LockAction, Variant = ConfirmDialogVariant.Warning })) return;
        IsSaving = true;
        try
        {
            _detail = await _api.LockAsync(_detail.Id, new(!_detail.IsActive));
            Editor = MakeEditor(_detail); NotifySelection(); await LoadAsync(); ShowSuccess("Account status updated");
        }
        catch (Exception ex) { DetailError = ex.Message; }
        finally { IsSaving = false; }
    }
    [RelayCommand] private void OpenResetPin() => OpenCredential(true);
    [RelayCommand] private void OpenResetPassword() => OpenCredential(false);
    private void OpenCredential(bool pin)
    {
        if (!CanSave()) return;
        _resetPin = pin; NewCredential = ""; CredentialError = null;
        CredentialTitle = pin ? "Reset PIN Pad" : "Reset Password";
    }
    [RelayCommand] private void CloseCredential() { if (!IsSaving) { NewCredential = ""; CredentialTitle = null; } }
    [RelayCommand(CanExecute = nameof(NotSaving))] private async Task SubmitCredentialAsync()
    {
        if (_detail == null || !CanManageSelection) return;
        if (_resetPin && !Regex.IsMatch(NewCredential, "\\A[0-9]{6}\\z")) { CredentialError = "PIN must contain exactly 6 digits (0–9)."; return; }
        if (string.IsNullOrWhiteSpace(NewCredential)) { CredentialError = "Enter the new credential."; return; }
        IsSaving = true;
        try
        {
            _detail = _resetPin ? await _api.ResetPinAsync(_detail.Id, new(NewCredential)) : await _api.ResetPasswordAsync(_detail.Id, new(NewCredential));
            NewCredential = ""; CredentialTitle = null; NotifySelection(); ShowSuccess("Credential reset successfully");
        }
        catch (Exception ex) { CredentialError = ex.Message; }
        finally { IsSaving = false; }
    }
    [RelayCommand] private void ClearFilters()
    { _suppressFilters = true; Search = ""; BranchId = null; RoleId = null; ActiveStatus = null; _suppressFilters = false; FilterChanged(); }
    private bool CanPrevious() => PageNumber > 1 && !IsLoading;
    private bool CanNext() => PageNumber < TotalPages && !IsLoading;
    [RelayCommand(CanExecute = nameof(CanPrevious))] private async Task PreviousPageAsync() { PageNumber--; await LoadAsync(); }
    [RelayCommand(CanExecute = nameof(CanNext))] private async Task NextPageAsync() { PageNumber++; await LoadAsync(); }
    private void NotifyPaging()
    {
        OnPropertyChanged(nameof(Showing)); OnPropertyChanged(nameof(PageSummary)); OnPropertyChanged(nameof(IsEmpty));
        PreviousPageCommand.NotifyCanExecuteChanged(); NextPageCommand.NotifyCanExecuteChanged();
    }
    private void NotifySelection()
    {
        foreach (var p in new[] { nameof(SelectedName), nameof(SelectedRole), nameof(SelectedUsername), nameof(SelectedStore), nameof(SelectedStatus), nameof(LockAction), nameof(CanManageSelection) }) OnPropertyChanged(p);
        SaveProfileCommand.NotifyCanExecuteChanged(); ToggleLockCommand.NotifyCanExecuteChanged();
    }
    private Task<bool> ConfirmDiscardAsync() => Editor?.IsDirty != true ? Task.FromResult(true) :
        _dialogs.ShowConfirmAsync(new() { Title = "Unsaved changes", Message = "Discard unsaved profile changes?", ConfirmText = "Discard", Variant = ConfirmDialogVariant.Warning });
    protected override void OnStoreChanged(string storeId, string? storeName)
    {
        if (!_initialized) return;
        _detailCts?.Cancel(); ++_detailVersion; IsDetailLoading = false;
        Editor = null; NewEmployee?.ClearSecrets(); NewEmployee = null; _detail = null; CredentialTitle = null; NewCredential = "";
        _suppressFilters = true; BranchId = null; RoleId = null; PageNumber = 1; _suppressFilters = false; _ = LoadAsync(true);
    }
    public override void Dispose()
    {
        _listCts?.Cancel(); _listCts?.Dispose(); _detailCts?.Cancel(); _detailCts?.Dispose();
        Editor?.ClearSecrets(); NewEmployee?.ClearSecrets(); NewCredential = ""; base.Dispose();
    }
}
