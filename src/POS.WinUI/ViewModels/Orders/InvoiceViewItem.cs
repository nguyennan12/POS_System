using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Orders;

public sealed record InvoiceLineItem(string SkuCode, string ProductName, decimal Quantity, decimal UnitPrice, decimal? LineTotal = null)
{
    public decimal Total => LineTotal ?? Quantity * UnitPrice;
    public string UnitPriceText => InvoiceViewItem.Money(UnitPrice);
    public string TotalText => InvoiceViewItem.Money(Total);
}

public sealed partial class InvoiceViewItem : ObservableObject
{
    public Guid Id { get; init; }
    public required string InvoiceNo { get; init; }
    public required string OrderNo { get; init; }
    public required DateTime IssuedAt { get; init; }
    public required string BuyerName { get; init; }
    public required string BuyerPhone { get; init; }
    public required string CashierName { get; init; }
    public required string PaymentMethod { get; init; }
    public string StoreName { get; init; } = "";
    public string StoreAddress { get; init; } = "";
    public required IReadOnlyList<InvoiceLineItem> Items { get; init; }
    public decimal Discount { get; init; }
    public decimal? SnapshotSubtotal { get; init; }
    public decimal? SnapshotTax { get; init; }
    public decimal? SnapshotGrandTotal { get; init; }
    public bool IsPreview { get; init; }
    public decimal Subtotal => SnapshotSubtotal ?? Items.Sum(x => x.Total);
    public decimal Tax => SnapshotTax ?? Math.Round((Subtotal - Discount) * 0.08m);
    public decimal GrandTotal => SnapshotGrandTotal ?? Subtotal - Discount + Tax;
    public string DateText => IssuedAt.ToString("dd/MM/yyyy");
    public string TimeText => IssuedAt.ToString("HH:mm");
    public string IssuedAtText => IssuedAt.ToString("dd/MM/yyyy · HH:mm");
    public string BuyerInitial => BuyerName[..1];
    public string SubtotalText => Money(Subtotal);
    public string DiscountText => "− " + Money(Discount);
    public string TaxText => Money(Tax);
    public string GrandTotalText => Money(GrandTotal);
    public string ItemCountText => $"{Items.Count} sản phẩm · {Items.Sum(x => x.Quantity):0.###} đơn vị";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReprintText))]
    private int _reprintCount;
    public string ReprintText => !IsPreview ? "In lại luôn có dấu BẢN SAO / REPRINT."
        : ReprintCount == 0 ? "Chưa in lại (minh họa)" : $"Đã in lại {ReprintCount} lần (minh họa)";
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string PaymentLabel(string method) => method switch
    {
        "Cash" => "Tiền mặt", "Card" => "Thẻ ngân hàng", "Points" => "Điểm thành viên", _ => method
    };
}
