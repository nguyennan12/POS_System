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

public record PermissionItemModel(string Code, string Name, string Description, bool IsGranted);

public record PermissionGroupModel(string GroupName, string Icon, IReadOnlyList<PermissionItemModel> Items)
{
    public int GrantedCount => Items.Count(i => i.IsGranted);
    public int TotalCount => Items.Count;
    public string SummaryText => $"{GrantedCount}/{TotalCount} quyền";
    public bool HasGrantedPermissions => GrantedCount > 0;
    public bool IsFullAccess => GrantedCount == TotalCount && TotalCount > 0;
    public IReadOnlyList<PermissionItemModel> GrantedItems => Items.Where(i => i.IsGranted).ToList();
}


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
    public IReadOnlyList<PermissionGroupModel> PermissionGroups => BuildPermissionGroups();
    public int TotalGrantedPermissionsCount => _roles.FirstOrDefault(r => r.Id == RoleId)?.Permissions.Count ?? 0;

    public string RoleDescription => Roles.FirstOrDefault(r => r.Id == RoleId)?.Name switch
    {
        "Owner" => "Chủ hệ thống: Có toàn quyền quản trị cao nhất trên tất cả chi nhánh, nhân sự, tài chính và cấu hình chuỗi.",
        "StoreManager" => "Cửa hàng trưởng: Quản lý vận hành chi nhánh, kho bãi, nhập hàng, kiểm kê và kiểm toán ca làm việc.",
        "Cashier" => "Thu ngân: Bán hàng tại quầy POS, in hóa đơn, mở/đóng ca và chăm sóc khách hàng.",
        _ => "Vai trò hệ thống phân quyền chi tiết theo chính sách cửa hàng."
    };

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
    partial void OnRoleIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(Permissions));
        OnPropertyChanged(nameof(PermissionGroups));
        OnPropertyChanged(nameof(TotalGrantedPermissionsCount));
        OnPropertyChanged(nameof(RoleDescription));
    }
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

    private IReadOnlyList<PermissionGroupModel> BuildPermissionGroups()
    {
        var granted = new HashSet<string>(_roles.FirstOrDefault(r => r.Id == RoleId)?.Permissions ?? [], StringComparer.OrdinalIgnoreCase);

        return
        [
            new PermissionGroupModel("Bán hàng & Thu ngân (POS)", "Cart24",
            [
                new("orders:create_own", "Tạo đơn & Thanh toán", "Thực hiện bán hàng và thanh toán tại quầy POS", granted.Contains("orders:create_own")),
                new("orders:read_own", "Xem đơn cá nhân", "Tra cứu lịch sử đơn hàng do chính mình tạo", granted.Contains("orders:read_own")),
                new("orders:read", "Xem toàn bộ đơn hàng", "Tra cứu đơn hàng của tất cả nhân viên trong cửa hàng", granted.Contains("orders:read")),
                new("orders:cancel_own", "Hủy đơn cá nhân", "Hủy đơn hàng do chính mình tạo khi chưa hoàn tất", granted.Contains("orders:cancel_own")),
                new("orders:cancel", "Hủy mọi đơn hàng", "Quyền can thiệp hủy bất kỳ đơn hàng nào", granted.Contains("orders:cancel")),
                new("orders:override_discount", "Chiết khấu linh hoạt", "Áp dụng giảm giá thủ công trực tiếp trên đơn hàng", granted.Contains("orders:override_discount")),
                new("orders:refund", "Trả hàng & Hoàn tiền", "Xử lý trả hàng và hoàn tiền cho khách", granted.Contains("orders:refund")),
                new("invoices:read", "Xem & In hóa đơn", "Tra cứu và in lại bản sao hóa đơn bán lẻ", granted.Contains("invoices:read")),
                new("shifts:manage_own", "Mở / Đóng ca cá nhân", "Bắt đầu và kết thúc ca thu ngân của mình", granted.Contains("shifts:manage_own")),
                new("shifts:read", "Xem lịch sử ca", "Xem danh sách và biên bản các ca làm việc", granted.Contains("shifts:read")),
                new("shifts:audit_approve", "Kiểm toán & Duyệt ca", "Đối soát tiền mặt và duyệt đóng ca của thu ngân", granted.Contains("shifts:audit_approve")),
                new("payments:create_own", "Thu tiền khách hàng", "Ghi nhận thanh toán tiền mặt, chuyển khoản, ví điện tử", granted.Contains("payments:create_own")),
                new("payments:read", "Xem giao dịch thanh toán", "Tra cứu chi tiết các giao dịch thanh toán", granted.Contains("payments:read"))
            ]),

            new PermissionGroupModel("Kho hàng & Nhập xuất tồn", "Box24",
            [
                new("inventory:read", "Tra cứu tồn kho", "Xem số lượng tồn thực tế, thẻ kho và hạn sử dụng", granted.Contains("inventory:read")),
                new("inventory:dispose", "Xuất hủy hàng hóa", "Lập phiếu xuất hủy hàng hư hỏng, hết date", granted.Contains("inventory:dispose")),
                new("stock_in_vouchers:read", "Xem phiếu nhập kho", "Tra cứu các phiếu nhập hàng từ nhà cung cấp", granted.Contains("stock_in_vouchers:read")),
                new("stock_in_vouchers:manage", "Tạo & Sửa phiếu nhập", "Tạo mới hoặc chỉnh sửa đơn nhập hàng NCC", granted.Contains("stock_in_vouchers:manage")),
                new("stock_in_vouchers:complete", "Xác nhận nhập kho", "Cộng dồn số lượng tồn và cập nhật giá vốn", granted.Contains("stock_in_vouchers:complete")),
                new("stock_takes:manage", "Tạo đợt kiểm kê", "Lập phiếu kiểm đếm số lượng hàng thực tế", granted.Contains("stock_takes:manage")),
                new("stock_takes:approve", "Duyệt cân bằng kho", "Cân chỉnh sai lệch giữa tồn thực tế và phần mềm", granted.Contains("stock_takes:approve")),
                new("products:read", "Xem sản phẩm", "Tra cứu danh mục sản phẩm, biến thể SKU và giá", granted.Contains("products:read")),
                new("products:manage", "Thêm / Sửa sản phẩm", "Cập nhật giá bán, thông tin hàng hóa và in mã vạch", granted.Contains("products:manage")),
                new("products:import", "Nhập Excel sản phẩm", "Nhập nhanh hàng loạt danh mục từ file Excel", granted.Contains("products:import")),
                new("categories:read", "Xem danh mục", "Xem cây phân cấp nhóm hàng và ngành hàng", granted.Contains("categories:read")),
                new("categories:manage", "Quản lý danh mục", "Thêm mới, đổi tên hoặc sắp xếp danh mục hàng", granted.Contains("categories:manage")),
                new("suppliers:read", "Xem nhà cung cấp", "Tra cứu danh sách và công nợ nhà cung cấp", granted.Contains("suppliers:read")),
                new("suppliers:manage", "Quản lý NCC", "Thêm mới và cập nhật thông tin đối tác cung cấp", granted.Contains("suppliers:manage")),
                new("suppliers:pay", "Thanh toán NCC", "Lập phiếu chi thanh toán tiền hàng cho NCC", granted.Contains("suppliers:pay"))
            ]),

            new PermissionGroupModel("Khách hàng & Hội viên", "People24",
            [
                new("customers:read", "Tra cứu khách hàng", "Tìm kiếm hồ sơ khách hàng theo SĐT, tên", granted.Contains("customers:read")),
                new("customers:create", "Tạo khách hàng mới", "Thêm nhanh hồ sơ khách hàng tại quầy POS", granted.Contains("customers:create")),
                new("customers:manage", "Cập nhật khách hàng", "Chỉnh sửa thông tin liên hệ và địa chỉ khách", granted.Contains("customers:manage")),
                new("customers:loyalty_adjust", "Điều chỉnh điểm thưởng", "Cộng / trừ điểm tích lũy thành viên thủ công", granted.Contains("customers:loyalty_adjust")),
                new("member_tiers:read", "Xem hạng thành viên", "Xem chính sách và ngưỡng chi tiêu các hạng thẻ", granted.Contains("member_tiers:read")),
                new("member_tiers:manage", "Cấu hình hạng thẻ", "Thiết lập quyền lợi và tỷ lệ tích điểm từng hạng", granted.Contains("member_tiers:manage")),
                new("promotions:read", "Xem khuyến mãi", "Tra cứu các chương trình giảm giá đang chạy", granted.Contains("promotions:read")),
                new("promotions:manage", "Quản lý khuyến mãi", "Tạo mới và bật / tắt chương trình khuyến mãi", granted.Contains("promotions:manage")),
                new("vouchers:read", "Xem mã Voucher", "Tra cứu danh sách mã ưu đãi giảm giá", granted.Contains("vouchers:read")),
                new("vouchers:apply_own", "Áp dụng Voucher", "Nhập mã voucher giảm giá cho khách tại quầy", granted.Contains("vouchers:apply_own")),
                new("vouchers:manage", "Phát hành Voucher", "Tạo mã giảm giá và cài đặt điều kiện áp dụng", granted.Contains("vouchers:manage"))
            ]),

            new PermissionGroupModel("Nhân sự & Phân quyền", "PersonAccounts24",
            [
                new("employees:read", "Xem danh sách nhân viên", "Tra cứu nhân sự thuộc chi nhánh hoặc toàn chuỗi", granted.Contains("employees:read")),
                new("employees:manage", "Thêm / Khóa nhân viên", "Tạo tài khoản, đặt lại mật khẩu/PIN, khóa truy cập", granted.Contains("employees:manage")),
                new("employees:login_history", "Lịch sử đăng nhập", "Xem nhật ký đăng nhập và thiết bị của nhân viên", granted.Contains("employees:login_history")),
                new("roles:manage", "Quản lý vai trò", "Cấu hình nhóm quyền và cấp bậc trong hệ thống", granted.Contains("roles:manage")),
                new("stores:read", "Xem chi nhánh", "Xem thông tin các cửa hàng trong chuỗi", granted.Contains("stores:read")),
                new("stores:manage", "Quản lý chi nhánh", "Thêm hoặc chỉnh sửa thông tin điểm bán", granted.Contains("stores:manage"))
            ]),

            new PermissionGroupModel("Báo cáo & Quản trị Hệ thống", "Settings24",
            [
                new("reports:dashboard_view", "Dashboard tổng quan", "Xem biểu đồ doanh số, đơn hàng và KPI", granted.Contains("reports:dashboard_view")),
                new("reports:revenue_view", "Báo cáo doanh thu", "Báo cáo chi tiết theo ngày, phương thức thanh toán", granted.Contains("reports:revenue_view")),
                new("reports:inventory_view", "Báo cáo tồn kho", "Báo cáo xuất nhập tồn và giá trị kho hàng", granted.Contains("reports:inventory_view")),
                new("reports:profit_view", "Báo cáo lợi nhuận", "Phân tích doanh thu, giá vốn và tỷ suất lợi nhuận", granted.Contains("reports:profit_view")),
                new("reports:export", "Xuất file báo cáo", "Xuất dữ liệu thống kê ra file Excel / PDF", granted.Contains("reports:export")),
                new("config:read", "Xem cấu hình", "Tra cứu các tham số thiết lập của hệ thống", granted.Contains("config:read")),
                new("config:manage_store", "Cấu hình cửa hàng", "Thiết lập máy in, mẫu hóa đơn, thông tin biên lai", granted.Contains("config:manage_store")),
                new("config:manage_global", "Cấu hình chuỗi", "Thiết lập chính sách tích điểm, thuế VAT toàn hệ thống", granted.Contains("config:manage_global")),
                new("audit_logs:read", "Nhật ký kiểm toán", "Tra cứu toàn bộ thao tác nhạy cảm của người dùng", granted.Contains("audit_logs:read"))
            ])
        ];
    }

}
