using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Products;
using POS.WinUI.ApiClients;
using POS.WinUI.Models;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductEditViewModel : ObservableObject
{
    private readonly ProductApiClient _productApi;
    private readonly CategoryApiClient _categoryApi;

    /// <summary>Raised khi form đóng — true = đã lưu thành công.</summary>
    public event Action<bool>? CloseRequested;

    // ── Mode ──────────────────────────────────────────────────────────────────
    private Guid? _editingProductId;
    public bool IsEditMode => _editingProductId.HasValue;
    public string WindowTitle => IsEditMode ? "Chỉnh sửa sản phẩm" : "Thêm sản phẩm mới";

    // ── Product fields ────────────────────────────────────────────────────────
    [ObservableProperty] private string _productName    = string.Empty;
    [ObservableProperty] private string _brand          = string.Empty;
    [ObservableProperty] private string _description    = string.Empty;
    [ObservableProperty] private string _baseUnit       = "Cái";
    [ObservableProperty] private string _imageUrl       = string.Empty;
    [ObservableProperty] private string _status         = "Active";

    // ── Categories ────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CategoryResponse> _categories = new();
    [ObservableProperty] private CategoryResponse? _selectedCategory;

    // ── SKUs ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<SkuEditItem> _skus = new();
    [ObservableProperty] private SkuEditItem? _selectedSku;

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private bool    _isSaving;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    // ── Barcode print ─────────────────────────────────────────────────────────
    [ObservableProperty] private int _printCopies = 1;

    public ProductEditViewModel(ProductApiClient productApi, CategoryApiClient categoryApi)
    {
        _productApi  = productApi;
        _categoryApi = categoryApi;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public async Task InitializeForCreateAsync()
    {
        _editingProductId = null;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(WindowTitle));

        await LoadCategoriesAsync();
        Skus.Clear();
        AddSkuRow();   // Bắt đầu với 1 dòng SKU trống
    }

    public async Task InitializeForEditAsync(Guid productId)
    {
        _editingProductId = productId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(WindowTitle));

        await LoadCategoriesAsync();
        await LoadProductAsync(productId);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var res = await _categoryApi.GetAllAsync();
            if (res?.Success == true && res.Data != null)
            {
                Categories.Clear();
                foreach (var c in res.Data)
                    Categories.Add(c);
            }
        }
        catch { /* silent */ }
    }

    private async Task LoadProductAsync(Guid id)
    {
        IsLoading = true;
        try
        {
            var res = await _productApi.GetProductByIdAsync(id);
            if (res?.Success == true && res.Data != null)
            {
                var p = res.Data;
                ProductName  = p.Name;
                Brand        = p.Brand ?? string.Empty;
                Description  = p.Description ?? string.Empty;
                BaseUnit     = p.BaseUnit;
                ImageUrl     = p.ImageUrl ?? string.Empty;
                Status       = p.Status;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == p.CategoryId);

                Skus.Clear();
                foreach (var s in p.Skus)
                {
                    // Parse attributes JSON -> Color/Size shortcut fields
                    string color = string.Empty, size = string.Empty;
                    if (s.Attributes.HasValue && s.Attributes.Value.ValueKind == JsonValueKind.Object)
                    {
                        if (s.Attributes.Value.TryGetProperty("color", out var cv))
                            color = cv.GetString() ?? string.Empty;
                        if (s.Attributes.Value.TryGetProperty("size", out var sv))
                            size = sv.GetString() ?? string.Empty;
                    }
                    Skus.Add(new SkuEditItem
                    {
                        Id        = s.Id,
                        SkuCode   = s.SkuCode,
                        Barcode   = s.Barcode,
                        CostPrice = s.CostPrice,
                        SellPrice = s.SellPrice,
                        TaxRate   = s.TaxRate,
                        IsActive  = s.IsActive,
                        Color     = color,
                        Size      = size,
                    });
                }
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không tải được dữ liệu sản phẩm.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── SKU Management ────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddSkuRow()
    {
        Skus.Add(new SkuEditItem
        {
            SkuCode = $"SKU-{DateTime.Now:mmssff}",
            Barcode = GenerateBarcode(),
        });
    }

    [RelayCommand]
    private void RemoveSkuRow(SkuEditItem? item)
    {
        if (item != null && Skus.Count > 1)
            Skus.Remove(item);
    }

    /// <summary>Tạo mã barcode EAN-13 tạm thời.</summary>
    private static string GenerateBarcode()
    {
        var rand = new Random();
        var digits = Enumerable.Range(0, 12).Select(_ => rand.Next(0, 10)).ToArray();
        int checksum = 0;
        for (int i = 0; i < 12; i++)
            checksum += digits[i] * (i % 2 == 0 ? 1 : 3);
        digits = [.. digits, (10 - checksum % 10) % 10];
        return string.Concat(digits);
    }

    // ── Barcode Preview ───────────────────────────────────────────────────────

    /// <summary>
    /// Sinh ảnh barcode từ mã EAN bằng ZXing.Net để preview trong UI.
    /// Trả null nếu barcode không hợp lệ.
    /// </summary>
    public static System.Windows.Media.Imaging.BitmapSource? GenerateBarcodeImage(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        try
        {
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width  = 300,
                    Height = 80,
                    Margin = 4,
                    PureBarcode = false,
                }
            };
            var pd = writer.Write(barcode);
            var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                pd.Width, pd.Height, 96, 96,
                System.Windows.Media.PixelFormats.Bgr32, null);
            bitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, pd.Width, pd.Height),
                pd.Pixels, pd.Width * 4, 0);
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }

    // ── Print Label ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void PrintBarcode(SkuEditItem? sku)
    {
        if (sku is null || string.IsNullOrWhiteSpace(sku.Barcode))
        {
            ErrorMessage = "Chọn SKU có mã vạch để in tem.";
            return;
        }

        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return;

        // Tạo visual in tem: Barcode + SKUCode + SellPrice
        var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(8) };
        var img = GenerateBarcodeImage(sku.Barcode);
        if (img != null)
            panel.Children.Add(new System.Windows.Controls.Image
            {
                Source = img,
                Width = 200,
                Height = 56,
                Stretch = System.Windows.Media.Stretch.Uniform,
            });

        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = sku.Barcode,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            FontSize = 10,
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"{sku.SellPrice:N0} đ",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = System.Windows.FontWeights.Bold,
        });

        for (int i = 0; i < PrintCopies; i++)
        {
            panel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            panel.Arrange(new System.Windows.Rect(panel.DesiredSize));
            dlg.PrintVisual(panel, $"Tem mã vạch – {sku.SkuCode}");
        }

        _ = ShowSuccessAsync($"Đã gửi {PrintCopies} tem đến máy in.");
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            if (IsEditMode)
                await UpdateProductAsync();
            else
                await CreateProductAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task CreateProductAsync()
    {
        var skuRequests = Skus.Select(s => new CreateSkuRequest(
            s.SkuCode,
            s.Barcode,
            s.CostPrice,
            s.SellPrice,
            s.TaxRate,
            BuildAttributes(s)
        )).ToList();

        var req = new CreateProductRequest(
            ProductName,
            SelectedCategory!.Id,
            BaseUnit,
            string.IsNullOrWhiteSpace(Description) ? null : Description,
            string.IsNullOrWhiteSpace(Brand) ? null : Brand,
            string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl,
            skuRequests
        );

        var res = await _productApi.CreateProductAsync(req);
        if (res?.Success == true)
        {
            await ShowSuccessAsync("Thêm sản phẩm thành công!");
            await Task.Delay(800);
            CloseRequested?.Invoke(true);
        }
        else
        {
            ErrorMessage = res?.Error?.Message ?? "Không tạo được sản phẩm.";
        }
    }

    private async Task UpdateProductAsync()
    {
        var req = new UpdateProductRequest(
            ProductName,
            SelectedCategory!.Id,
            BaseUnit,
            string.IsNullOrWhiteSpace(Description) ? null : Description,
            string.IsNullOrWhiteSpace(Brand) ? null : Brand,
            string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl,
            Status
        );

        var res = await _productApi.UpdateProductAsync(_editingProductId!.Value, req);
        if (res?.Success == true)
        {
            await ShowSuccessAsync("Cập nhật thành công!");
            await Task.Delay(800);
            CloseRequested?.Invoke(true);
        }
        else
        {
            ErrorMessage = res?.Error?.Message ?? "Không cập nhật được sản phẩm.";
        }
    }

    private static JsonElement? BuildAttributes(SkuEditItem s)
    {
        if (string.IsNullOrWhiteSpace(s.Color) && string.IsNullOrWhiteSpace(s.Size))
            return null;
        var dict = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(s.Color)) dict["color"] = s.Color;
        if (!string.IsNullOrWhiteSpace(s.Size))  dict["size"]  = s.Size;
        return JsonSerializer.SerializeToElement(dict);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool Validate()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(ProductName))
        { ErrorMessage = "Tên sản phẩm không được để trống."; return false; }
        if (SelectedCategory is null)
        { ErrorMessage = "Vui lòng chọn danh mục."; return false; }
        if (string.IsNullOrWhiteSpace(BaseUnit))
        { ErrorMessage = "Đơn vị cơ bản không được để trống."; return false; }
        if (!IsEditMode && Skus.Count == 0)
        { ErrorMessage = "Sản phẩm phải có ít nhất 1 biến thể SKU."; return false; }
        foreach (var s in Skus)
        {
            if (string.IsNullOrWhiteSpace(s.SkuCode))
            { ErrorMessage = $"Mã SKU không được để trống."; return false; }
            if (string.IsNullOrWhiteSpace(s.Barcode))
            { ErrorMessage = $"Mã vạch SKU '{s.SkuCode}' không được để trống."; return false; }
            if (s.SellPrice <= 0)
            { ErrorMessage = $"Giá bán SKU '{s.SkuCode}' phải lớn hơn 0."; return false; }
        }
        return true;
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    // ── Toast helper ──────────────────────────────────────────────────────────

    private CancellationTokenSource? _toastCts;

    private async Task ShowSuccessAsync(string msg, int ms = 2000)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        SuccessMessage = msg;
        ErrorMessage   = null;
        try { await Task.Delay(ms, _toastCts.Token); } catch { }
        SuccessMessage = null;
    }
}
