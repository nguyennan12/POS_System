using System;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Products;

public sealed partial class ProductSkuItemModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.Empty;

    [ObservableProperty]
    private Guid _productId = Guid.Empty;

    [ObservableProperty]
    private string _skuCode = string.Empty;

    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private decimal _costPrice;

    [ObservableProperty]
    private decimal _sellPrice;

    [ObservableProperty]
    private decimal _taxRate;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private decimal _qtyOnHand;

    public string FormattedCostPrice => $"{CostPrice:N0} đ";
    public string FormattedSellPrice => $"{SellPrice:N0} đ";
    public string FormattedQty => $"{QtyOnHand:N0}";

    public string AttributesDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Color) && !string.IsNullOrWhiteSpace(Size))
                return $"{Color} / {Size}";
            if (!string.IsNullOrWhiteSpace(Color))
                return Color;
            if (!string.IsNullOrWhiteSpace(Size))
                return Size;
            return "Mặc định";
        }
    }

    public string ProfitMargin
    {
        get
        {
            if (SellPrice <= 0) return "0%";
            var margin = (SellPrice - CostPrice) / SellPrice * 100m;
            return $"{margin:F1}%";
        }
    }

    partial void OnCostPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(FormattedCostPrice));
        OnPropertyChanged(nameof(ProfitMargin));
    }

    partial void OnSellPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(FormattedSellPrice));
        OnPropertyChanged(nameof(ProfitMargin));
    }

    partial void OnColorChanged(string value) => OnPropertyChanged(nameof(AttributesDisplay));
    partial void OnSizeChanged(string value) => OnPropertyChanged(nameof(AttributesDisplay));
    partial void OnQtyOnHandChanged(decimal value) => OnPropertyChanged(nameof(FormattedQty));

    public JsonElement? BuildAttributesJson()
    {
        if (string.IsNullOrWhiteSpace(Color) && string.IsNullOrWhiteSpace(Size))
            return null;

        var dict = new System.Collections.Generic.Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Color)) dict["color"] = Color.Trim();
        if (!string.IsNullOrWhiteSpace(Size)) dict["size"] = Size.Trim();

        return JsonSerializer.SerializeToElement(dict);
    }

    public static (string color, string size) ParseAttributes(JsonElement? attributes)
    {
        if (attributes is null || attributes.Value.ValueKind != JsonValueKind.Object)
            return (string.Empty, string.Empty);

        string color = string.Empty;
        string size = string.Empty;

        if (attributes.Value.TryGetProperty("color", out var cProp))
            color = cProp.GetString() ?? string.Empty;
        else if (attributes.Value.TryGetProperty("mau", out var mProp))
            color = mProp.GetString() ?? string.Empty;

        if (attributes.Value.TryGetProperty("size", out var sProp))
            size = sProp.GetString() ?? string.Empty;
        else if (attributes.Value.TryGetProperty("kich_co", out var kcProp))
            size = kcProp.GetString() ?? string.Empty;

        return (color, size);
    }
}
