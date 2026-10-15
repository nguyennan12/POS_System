using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZXing;
using ZXing.Common;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductsViewModel
{
    // ── Trạng thái Modal In Tem Mã Vạch ──
    [ObservableProperty]
    private bool _isBarcodeModalOpen = false;

    [ObservableProperty]
    private string _barcodeProductName = string.Empty;

    [ObservableProperty]
    private string _barcodeSkuCode = string.Empty;

    [ObservableProperty]
    private string _barcodeNumber = string.Empty;

    [ObservableProperty]
    private string _barcodeColor = string.Empty;

    [ObservableProperty]
    private string _barcodeSize = string.Empty;

    [ObservableProperty]
    private decimal _barcodeSellPrice = 0;

    public string BarcodeSellPriceFormatted => $"{BarcodeSellPrice:N0} đ";

    public string BarcodeVariantText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BarcodeColor) && !string.IsNullOrWhiteSpace(BarcodeSize))
                return $"{BarcodeColor} / {BarcodeSize}";
            if (!string.IsNullOrWhiteSpace(BarcodeColor)) return BarcodeColor;
            if (!string.IsNullOrWhiteSpace(BarcodeSize)) return BarcodeSize;
            return "Tiêu chuẩn";
        }
        set
        {
            // Cho phép WPF binding không gây lỗi khi TwoWay
        }
    }

    [ObservableProperty]
    private int _barcodePrintQuantity = 1;

    [ObservableProperty]
    private BitmapSource? _barcodeImage;

    [ObservableProperty]
    private string _barcodeStoreName = "CỬA HÀNG BÁN LẺ POS";

    private void InitializeBarcode()
    {
        if (!string.IsNullOrWhiteSpace(CurrentStoreName))
        {
            BarcodeStoreName = CurrentStoreName.ToUpperInvariant();
        }
    }

    [RelayCommand]
    public void OpenBarcodeModal(ProductSkuItemModel? sku)
    {
        if (sku == null) return;

        // Tìm tên sản phẩm cha
        var parentProduct = Products.FirstOrDefault(p => p.Id == sku.ProductId);
        var prodName = parentProduct?.Name ?? (SelectedProduct?.Name ?? "Sản phẩm");

        BarcodeProductName = prodName;
        BarcodeSkuCode = sku.SkuCode;
        BarcodeNumber = sku.Barcode;
        BarcodeColor = sku.Color;
        BarcodeSize = sku.Size;
        BarcodeSellPrice = sku.SellPrice;
        BarcodePrintQuantity = 1;

        if (!string.IsNullOrWhiteSpace(CurrentStoreName))
            BarcodeStoreName = CurrentStoreName.ToUpperInvariant();

        OnPropertyChanged(nameof(BarcodeSellPriceFormatted));
        OnPropertyChanged(nameof(BarcodeVariantText));

        // Sinh hình ảnh mã vạch Code 128
        BarcodeImage = GenerateCode128Barcode(BarcodeNumber, 320, 80);

        IsBarcodeModalOpen = true;
    }

    [RelayCommand]
    public void OpenBarcodeModalForProduct(ProductItemModel? product)
    {
        if (product == null) return;
        SelectedProduct = product;

        if (product.Skus.Count > 0)
        {
            OpenBarcodeModal(product.Skus[0]);
        }
        else
        {
            // Tải SKU rồi mở
            _ = LoadProductAndOpenBarcodeAsync(product);
        }
    }

    private async Task LoadProductAndOpenBarcodeAsync(ProductItemModel product)
    {
        await ToggleProductExpandAsync(product);
        if (product.Skus.Count > 0)
        {
            OpenBarcodeModal(product.Skus[0]);
        }
        else
        {
            ShowStatus($"Sản phẩm '{product.Name}' chưa có biến thể SKU để in tem.", isError: true);
        }
    }

    [RelayCommand]
    public void CloseBarcodeModal()
    {
        IsBarcodeModalOpen = false;
        BarcodeImage = null;
    }

    [RelayCommand]
    public void PrintBarcodeLabels(FrameworkElement? visualElement)
    {
        try
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                if (visualElement != null)
                {
                    printDlg.PrintVisual(visualElement, $"In tem mã vạch - {BarcodeSkuCode}");
                    ShowStatus($"Đã gửi lệnh in tem cho SKU '{BarcodeSkuCode}'.");
                }
                else
                {
                    ShowStatus("Không thể in: visual element null", isError: true);
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi in tem mã vạch: {ex.Message}", isError: true);
        }
    }

    public static BitmapSource? GenerateCode128Barcode(string content, int width = 300, int height = 80)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 4,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content.Trim());
            var bmp = BitmapSource.Create(
                pixelData.Width,
                pixelData.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixelData.Pixels,
                pixelData.Width * 4);

            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
