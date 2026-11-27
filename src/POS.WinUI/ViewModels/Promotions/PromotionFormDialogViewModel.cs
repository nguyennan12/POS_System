using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Promotions;

namespace POS.WinUI.ViewModels.Promotions;

public record PromotionTypeOption(string Code, string DisplayName);

public sealed partial class PromotionTargetOption(Guid id, string displayName, bool isSelected = false) : ObservableObject
{
    public Guid Id { get; } = id;
    public string DisplayName { get; } = displayName;

    [ObservableProperty] private bool _isSelected = isSelected;
}

public sealed partial class PromotionFormDialogViewModel : ObservableObject
{
    private const string CartPercent = "CartPercent";
    private const string CartFixed = "CartFixed";
    private const string PercentSku = "PercentSku";
    private const string FixedSku = "FixedSku";
    private const string BuyXGetY = "BuyXGetY";
    private const string HappyHour = "HappyHour";
    private const string All = "All";
    private const string Category = "Category";
    private const string Sku = "SKU";

    private readonly PromotionDetailResponse? _original;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public Guid? Id => _original?.Id;
    public Guid? CatalogStoreId { get; }
    public bool IsEdit => Id.HasValue;
    public string DialogTitle => IsEdit ? "Cập nhật chương trình khuyến mãi" : "Tạo chương trình khuyến mãi mới";
    public string DialogSubtitle => IsEdit ? "Điều chỉnh mức giảm giá, điều kiện áp dụng và thời hạn" : "Thiết lập quy tắc và thời hạn áp dụng ưu đãi";

    public IReadOnlyList<PromotionTypeOption> TypeOptions { get; } =
    [
        new(CartPercent, "Giảm theo đơn (%)"),
        new(CartFixed, "Giảm theo đơn (VNĐ)"),
        new(PercentSku, "Giảm theo sản phẩm (%)"),
        new(FixedSku, "Giảm theo sản phẩm (VNĐ)"),
        new(BuyXGetY, "Mua X tặng Y"),
        new(HappyHour, "Giờ vàng")
    ];

    public IReadOnlyList<PromotionTypeOption> ScopeOptions { get; } =
    [
        new(All, "Tất cả sản phẩm"),
        new(Category, "Danh mục"),
        new(Sku, "SKU")
    ];

    public bool IsBuyXGetY => SelectedType.Code == BuyXGetY;
    public bool IsHappyHour => SelectedType.Code == HappyHour;
    public bool HasDiscountValue => !IsBuyXGetY;
    public string DiscountValueLabel => IsPercent ? "Mức giảm (%) *" : "Mức giảm (VNĐ) *";
    public bool UsesCategoryTargets => SelectedScope.Code == Category;
    public bool UsesSkuTargets => SelectedScope.Code == Sku;
    public IEnumerable<PromotionTargetOption> FilteredSkuTargets =>
        string.IsNullOrWhiteSpace(SkuSearch)
            ? SkuTargets
            : SkuTargets.Where(option => option.DisplayName.Contains(SkuSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool IsPercent => SelectedType.Code is CartPercent or PercentSku or HappyHour;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuyXGetY), nameof(IsHappyHour), nameof(HasDiscountValue), nameof(DiscountValueLabel), nameof(ConditionLoadWarning))]
    private PromotionTypeOption _selectedType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesCategoryTargets), nameof(UsesSkuTargets))]
    private PromotionTypeOption _selectedScope;

    [ObservableProperty] private string _valueText = string.Empty;
    [ObservableProperty] private string _minOrderAmountText = "0";
    [ObservableProperty] private string? _maxDiscountAmountText;
    [ObservableProperty] private DateTime? _validFrom = DateTime.Today;
    [ObservableProperty] private DateTime? _validTo;
    [ObservableProperty] private string _priorityText = "0";
    [ObservableProperty] private bool _isStackable;
    [ObservableProperty] private bool _isExclusive;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isLoadingCatalog;
    [ObservableProperty] private string? _catalogMessage;
    [ObservableProperty] private IReadOnlyList<PromotionTargetOption> _categoryTargets = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSkuTargets))]
    private IReadOnlyList<PromotionTargetOption> _skuTargets = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSkuTargets))]
    private string _skuSearch = string.Empty;

    public PromotionFormDialogViewModel(PromotionDetailResponse? promo = null, Guid? catalogStoreId = null)
    {
        _original = promo;
        CatalogStoreId = catalogStoreId;
        _selectedType = TypeOptions.FirstOrDefault(t => string.Equals(t.Code, promo?.Type, StringComparison.OrdinalIgnoreCase))
            ?? TypeOptions[0];
        _selectedScope = ScopeOptions.FirstOrDefault(s => string.Equals(s.Code, promo?.AppliesTo, StringComparison.OrdinalIgnoreCase))
            ?? ScopeOptions[0];

        if (promo is not null)
        {
            Name = promo.Name;
            ValueText = promo.Value.ToString(_culture);
            MinOrderAmountText = promo.MinOrderAmount.ToString(_culture);
            MaxDiscountAmountText = promo.MaxDiscountAmount?.ToString(_culture);
            ValidFrom = promo.ValidFrom.LocalDateTime.Date;
            ValidTo = promo.ValidTo?.LocalDateTime.Date;
            PriorityText = promo.Priority.ToString(_culture);
            IsStackable = promo.IsStackable;
            IsExclusive = promo.IsExclusive;
            CategoryTargets = MergeTargets([], promo.TargetCategoryIds, "Danh mục");
            SkuTargets = MergeTargets([], promo.TargetSkuIds, "SKU");
            LoadConditions(promo.ConditionsJson);
        }

        SetSkuCatalog([]);
    }

    internal void SetCategoryCatalog(IEnumerable<PromotionTargetOption> options) =>
        CategoryTargets = MergeTargets(options, SelectedIds(CategoryTargets), "Danh mục");

    internal void SetSkuCatalog(IEnumerable<PromotionTargetOption> options)
    {
        var catalog = options.DistinctBy(option => option.Id).ToList();
        SkuTargets = MergeTargets(catalog, SelectedIds(SkuTargets), "SKU");

        var giftId = FreeSkuId;
        var giftOptions = new List<PromotionTargetOption> { new(Guid.Empty, "Cùng SKU với sản phẩm mua") };
        giftOptions.AddRange(catalog);
        if (giftId is Guid id && id != Guid.Empty && catalog.All(option => option.Id != id))
            giftOptions.Add(UnavailableTarget(id, "SKU", isSelected: false));

        FreeSkuOptions = giftOptions;
        FreeSkuId = giftId;
    }

    public bool TryCreateRequest([NotNullWhen(true)] out CreatePromotionRequest? request)
    {
        request = null;
        if (!TryGetRequestFields(out var fields))
            return false;

        request = new CreatePromotionRequest(
            Name.Trim(), SelectedType.Code, fields.Value, fields.Minimum, fields.Maximum,
            fields.ConditionsJson, fields.Priority, IsStackable, IsExclusive,
            SelectedScope.Code, fields.From, fields.To, fields.Categories, fields.Skus);
        return true;
    }

    public bool TryUpdateRequest([NotNullWhen(true)] out UpdatePromotionRequest? request)
    {
        request = null;
        if (_original is null || !TryGetRequestFields(out var fields))
            return false;

        request = new UpdatePromotionRequest(
            Name.Trim(), SelectedType.Code, fields.Value, fields.Minimum, fields.Maximum,
            fields.ConditionsJson, fields.Priority, IsStackable, IsExclusive,
            SelectedScope.Code, fields.From, fields.To, _original.Status, fields.Categories, fields.Skus);
        return true;
    }

    private bool TryGetRequestFields([NotNullWhen(true)] out RequestFields? fields)
    {
        fields = null;
        ErrorMessage = null;

        if (Name.Trim().Length is < 3 or > 200)
            return Invalid("Tên chương trình khuyến mãi phải có từ 3 đến 200 ký tự.");

        // Backend requires a positive Value for every type. BuyXGetY uses its condition for the actual discount.
        var value = IsSameTypeAsOriginal && _original is not null ? _original.Value : 1m;
        if (HasDiscountValue && (!TryDecimal(ValueText, out value) || value <= 0))
            return Invalid("Mức giảm giá phải là số dương.");
        if (IsPercent && value > 100)
            return Invalid("Mức giảm theo phần trăm không được vượt quá 100%.");
        if (!TryDecimal(MinOrderAmountText, out var minimum) || minimum < 0)
            return Invalid("Giá trị đơn hàng tối thiểu phải là số không âm.");

        decimal? maximum = null;
        if (!string.IsNullOrWhiteSpace(MaxDiscountAmountText))
        {
            if (!TryDecimal(MaxDiscountAmountText, out var parsedMaximum) || parsedMaximum <= 0)
                return Invalid("Mức giảm tối đa phải là số dương.");
            maximum = parsedMaximum;
        }

        if (!int.TryParse(PriorityText, NumberStyles.Integer, _culture, out var priority))
            return Invalid("Độ ưu tiên phải là số nguyên.");

        var categories = UsesCategoryTargets ? SelectedIds(CategoryTargets) : [];
        var skus = UsesSkuTargets ? SelectedIds(SkuTargets) : [];
        if (UsesCategoryTargets && categories.Count == 0)
            return Invalid("Chọn ít nhất một danh mục áp dụng.");
        if (UsesSkuTargets && skus.Count == 0)
            return Invalid("Chọn ít nhất một SKU áp dụng.");

        var from = GetValidityDate(ValidFrom, _original?.ValidFrom, endOfDay: false);
        var to = GetValidityDate(ValidTo, _original?.ValidTo, endOfDay: true);
        if (from.HasValue && to.HasValue && to < from)
            return Invalid("Ngày kết thúc chương trình phải lớn hơn hoặc bằng ngày bắt đầu.");

        if (!TryBuildConditionsJson(out var conditionsJson))
            return false;

        fields = new RequestFields(value, minimum, maximum, priority, from, to, categories, skus, conditionsJson);
        return true;
    }

    private bool Invalid(string message)
    {
        ErrorMessage = message;
        return false;
    }

    private bool TryDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, _culture, out value);

    private bool IsSameTypeAsOriginal =>
        string.Equals(_original?.Type, SelectedType.Code, StringComparison.OrdinalIgnoreCase);

    private static DateTimeOffset? GetValidityDate(DateTime? date, DateTimeOffset? original, bool endOfDay)
    {
        if (date is null)
            return null;
        if (date.Value.Date == original?.LocalDateTime.Date)
            return original;

        var localDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local);
        return new DateTimeOffset(endOfDay ? localDate.AddTicks(TimeSpan.TicksPerDay - 1) : localDate);
    }

    private static List<Guid> SelectedIds(IEnumerable<PromotionTargetOption> options) =>
        options.Where(option => option.IsSelected).Select(option => option.Id).ToList();

    private static IReadOnlyList<PromotionTargetOption> MergeTargets(
        IEnumerable<PromotionTargetOption> catalog, IEnumerable<Guid> ids, string label)
    {
        var selected = ids.ToHashSet();
        var options = catalog.DistinctBy(option => option.Id)
            .Select(option => new PromotionTargetOption(option.Id, option.DisplayName, selected.Remove(option.Id)))
            .ToList();
        options.AddRange(selected.Select(id => UnavailableTarget(id, label, isSelected: true)));
        return options;
    }

    private static PromotionTargetOption UnavailableTarget(Guid id, string label, bool isSelected) =>
        new(id, $"{label} đã lưu ({id})", isSelected);

    private sealed record RequestFields(
        decimal Value, decimal Minimum, decimal? Maximum, int Priority, DateTimeOffset? From,
        DateTimeOffset? To, IReadOnlyList<Guid> Categories, IReadOnlyList<Guid> Skus, string? ConditionsJson);
}
