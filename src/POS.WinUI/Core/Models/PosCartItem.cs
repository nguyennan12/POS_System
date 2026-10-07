using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.Core.Models;

/// <summary>
/// Model món hàng trong giỏ bán hàng POS Cashier
/// </summary>
public partial class PosCartItem : ObservableObject
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Unit { get; set; } = "Cái";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private int _quantity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _discountAmount = 0;

    public decimal SubTotal => Math.Max(0, (Price * Quantity) - DiscountAmount);
}
