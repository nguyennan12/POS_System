using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Products;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductsViewModel
{
    // ── Trạng thái Modal Tạo/Sửa Sản Phẩm ──
    [ObservableProperty]
    private bool _isProductEditModalOpen = false;

    [ObservableProperty]
    private bool _isEditMode = false;

    public string ProductModalTitle => IsEditMode ? "Chỉnh sửa sản phẩm & SKU" : "Thêm mới sản phẩm & Biến thể";

    // ── Dữ liệu form sản phẩm ──
    [ObservableProperty]
    private Guid _editProductId = Guid.Empty;

    [ObservableProperty]
    private string _editProductName = string.Empty;

    [ObservableProperty]
    private Guid _editCategoryId = Guid.Empty;

    [ObservableProperty]
    private CategoryItemModel? _editSelectedCategory;

    [ObservableProperty]
    private string _editBaseUnit = "Cái";

    [ObservableProperty]
    private string _editBrand = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editImageUrl = string.Empty;

    [ObservableProperty]
    private string _editStatus = "Active";

    [ObservableProperty]
    private string _editErrorMessage = string.Empty;

    // ── Danh sách SKU trong modal ──
    public ObservableCollection<ProductSkuItemModel> EditingSkus { get; } = new();

    // ── Công cụ sinh biến thể tự động cho Cửa hàng tiện lợi (Quy cách bán & Hương vị / Phân loại) ──
    [ObservableProperty]
    private string _variantColorInput = string.Empty; // Quy cách / Dung tích / Đóng gói

    [ObservableProperty]
    private string _variantSizeInput = string.Empty;  // Hương vị / Phân loại

    [ObservableProperty]
    private decimal _variantDefaultCost = 0;

    [ObservableProperty]
    private decimal _variantDefaultSell = 0;

    [ObservableProperty]
    private decimal _variantDefaultTax = 0;

    [RelayCommand]
    public void ApplyDrinkPreset()
    {
        VariantColorInput = "Lon 330ml, Chai 1.5L, Lốc 6 lon";
        VariantSizeInput = "Nguyên bản, Không đường";
        VariantDefaultCost = 10000;
        VariantDefaultSell = 15000;
        VariantDefaultTax = 8;
        EditingSkus.Clear();
        GenerateVariants();
    }

    [RelayCommand]
    public void ApplyNoodlePreset()
    {
        VariantColorInput = "Gói lẻ 75g, Tô 85g, Thùng 30 gói";
        VariantSizeInput = "Vị Bò cay, Tôm chua cay";
        VariantDefaultCost = 8000;
        VariantDefaultSell = 12000;
        VariantDefaultTax = 8;
        EditingSkus.Clear();
        GenerateVariants();
    }

    [RelayCommand]
    public void ApplyBeerPreset()
    {
        VariantColorInput = "Lon 330ml, Lốc 6 lon, Thùng 24 lon";
        VariantSizeInput = "Bạc, Vàng";
        VariantDefaultCost = 14000;
        VariantDefaultSell = 18000;
        VariantDefaultTax = 10;
        EditingSkus.Clear();
        GenerateVariants();
    }

    [RelayCommand]
    public void ApplyConsumerPreset()
    {
        VariantColorInput = "Gói nhỏ, Gói lớn";
        VariantSizeInput = "Tiêu chuẩn, Hương hoa";
        VariantDefaultCost = 25000;
        VariantDefaultSell = 35000;
        VariantDefaultTax = 10;
        EditingSkus.Clear();
        GenerateVariants();
    }

    [RelayCommand]
    public void OpenCreateProductModal()
    {
        IsEditMode = false;
        EditProductId = Guid.Empty;
        EditProductName = string.Empty;
        EditBaseUnit = string.Empty;
        EditBrand = string.Empty;
        EditDescription = string.Empty;
        EditImageUrl = string.Empty;
        EditStatus = "Active";
        EditErrorMessage = string.Empty;

        // Mặc định chọn danh mục đầu tiên hợp lệ
        EditSelectedCategory = CategoryFilterOptions.FirstOrDefault(c => c.Id != Guid.Empty);
        EditCategoryId = EditSelectedCategory?.Id ?? Guid.Empty;

        // Reset biến thể: để trống, chỉ hiển thị khi người dùng nhập hoặc bấm nút gợi ý
        EditingSkus.Clear();
        VariantColorInput = string.Empty;
        VariantSizeInput = string.Empty;
        VariantDefaultCost = 0;
        VariantDefaultSell = 0;
        VariantDefaultTax = 0;

        OnPropertyChanged(nameof(ProductModalTitle));
        IsProductEditModalOpen = true;
    }

    [RelayCommand]
    public async Task OpenEditProductModalAsync(ProductItemModel? product)
    {
        if (product == null) return;

        IsEditMode = true;
        EditProductId = product.Id;
        EditProductName = product.Name;
        EditBaseUnit = product.BaseUnit;
        EditBrand = product.Brand ?? string.Empty;
        EditImageUrl = product.ImageUrl ?? string.Empty;
        EditStatus = product.Status;
        EditErrorMessage = string.Empty;

        EditSelectedCategory = CategoryFilterOptions.FirstOrDefault(c => c.Id == product.CategoryId);
        EditCategoryId = product.CategoryId;

        EditingSkus.Clear();

        try
        {
            var detailRes = await _productApi.GetProductByIdAsync(product.Id);
            if (detailRes?.Data != null)
            {
                var detail = detailRes.Data;
                EditDescription = detail.Description ?? string.Empty;

                foreach (var s in detail.Skus)
                {
                    var (color, size) = ProductSkuItemModel.ParseAttributes(s.Attributes);
                    EditingSkus.Add(new ProductSkuItemModel
                    {
                        Id = s.Id,
                        ProductId = s.ProductId,
                        SkuCode = s.SkuCode,
                        Barcode = s.Barcode,
                        CostPrice = s.CostPrice,
                        SellPrice = s.SellPrice,
                        TaxRate = s.TaxRate,
                        IsActive = s.IsActive,
                        Color = color,
                        Size = size
                    });
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi tải chi tiết: {ex.Message}", isError: true);
        }

        if (EditingSkus.Count == 0)
        {
            AddDefaultSku();
        }

        OnPropertyChanged(nameof(ProductModalTitle));
        IsProductEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseProductEditModal()
    {
        IsProductEditModalOpen = false;
        EditingSkus.Clear();
    }

    private void AddDefaultSku()
    {
        var code = GeneratePrefixFromProductName(EditProductName);
        var barcode = GenerateRandomBarcode();
        EditingSkus.Add(new ProductSkuItemModel
        {
            Id = Guid.Empty,
            ProductId = EditProductId,
            SkuCode = string.IsNullOrWhiteSpace(code) ? "SKU-01" : $"{code}-01",
            Barcode = barcode,
            CostPrice = VariantDefaultCost,
            SellPrice = VariantDefaultSell,
            TaxRate = VariantDefaultTax,
            IsActive = true,
            Color = "Quy cách chuẩn",
            Size = "Nguyên bản"
        });
    }

    [RelayCommand]
    public void AddEmptySku()
    {
        var prefix = GeneratePrefixFromProductName(EditProductName);
        if (string.IsNullOrWhiteSpace(prefix)) prefix = "SKU";
        var index = EditingSkus.Count + 1;

        EditingSkus.Add(new ProductSkuItemModel
        {
            Id = Guid.Empty,
            ProductId = EditProductId,
            SkuCode = $"{prefix}-{index:D2}",
            Barcode = GenerateRandomBarcode(),
            CostPrice = VariantDefaultCost,
            SellPrice = VariantDefaultSell,
            TaxRate = VariantDefaultTax,
            IsActive = true,
            Color = "Quy cách chuẩn",
            Size = "Nguyên bản"
        });
    }

    [RelayCommand]
    public void RemoveSku(ProductSkuItemModel? sku)
    {
        if (sku == null) return;
        EditingSkus.Remove(sku);
    }

    [RelayCommand]
    public void GenerateBarcodeForSku(ProductSkuItemModel? sku)
    {
        if (sku == null) return;
        sku.Barcode = GenerateRandomBarcode();
    }

    /// <summary>
    /// Thuật toán sinh biến thể động cho Cửa hàng tiện lợi (Quy cách / Dung tích x Hương vị / Phân loại)
    /// </summary>
    [RelayCommand]
    public void GenerateVariants()
    {
        var colors = VariantColorInput
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        var sizes = VariantSizeInput
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (colors.Count == 0 && sizes.Count == 0)
        {
            ShowError("Vui lòng nhập ít nhất 1 quy cách/dung tích hoặc 1 hương vị/loại sản phẩm.");
            return;
        }
        var prefix = GeneratePrefixFromProductName(EditProductName);
        if (string.IsNullOrWhiteSpace(prefix)) prefix = "SP";

        // Trường hợp chỉ có màu
        if (colors.Count > 0 && sizes.Count == 0)
        {
            foreach (var c in colors)
            {
                var colorSlug = ToSlug(c).ToUpperInvariant();
                var skuCode = $"{prefix}-{colorSlug}";
                if (EditingSkus.Any(s => s.SkuCode == skuCode)) continue;

                EditingSkus.Add(new ProductSkuItemModel
                {
                    Id = Guid.Empty,
                    ProductId = EditProductId,
                    SkuCode = skuCode,
                    Barcode = GenerateRandomBarcode(),
                    Color = c,
                    Size = string.Empty,
                    CostPrice = VariantDefaultCost,
                    SellPrice = VariantDefaultSell,
                    TaxRate = VariantDefaultTax,
                    IsActive = true
                });
            }
        }
        // Trường hợp chỉ có size
        else if (colors.Count == 0 && sizes.Count > 0)
        {
            foreach (var s in sizes)
            {
                var sizeSlug = ToSlug(s).ToUpperInvariant();
                var skuCode = $"{prefix}-{sizeSlug}";
                if (EditingSkus.Any(item => item.SkuCode == skuCode)) continue;

                EditingSkus.Add(new ProductSkuItemModel
                {
                    Id = Guid.Empty,
                    ProductId = EditProductId,
                    SkuCode = skuCode,
                    Barcode = GenerateRandomBarcode(),
                    Color = string.Empty,
                    Size = s,
                    CostPrice = VariantDefaultCost,
                    SellPrice = VariantDefaultSell,
                    TaxRate = VariantDefaultTax,
                    IsActive = true
                });
            }
        }
        // Trường hợp tích hợp cả Màu x Size (Cartesian Product)
        else
        {
            foreach (var c in colors)
            {
                var colorSlug = ToSlug(c).ToUpperInvariant();
                foreach (var s in sizes)
                {
                    var sizeSlug = ToSlug(s).ToUpperInvariant();
                    var skuCode = $"{prefix}-{colorSlug}-{sizeSlug}";
                    if (EditingSkus.Any(item => item.SkuCode == skuCode)) continue;

                    EditingSkus.Add(new ProductSkuItemModel
                    {
                        Id = Guid.Empty,
                        ProductId = EditProductId,
                        SkuCode = skuCode,
                        Barcode = GenerateRandomBarcode(),
                        Color = c,
                        Size = s,
                        CostPrice = VariantDefaultCost,
                        SellPrice = VariantDefaultSell,
                        TaxRate = VariantDefaultTax,
                        IsActive = true
                    });
                }
            }
        }

        // Xóa SKU mặc định nếu có nhiều hơn 1 SKU và SKU mặc định chưa chỉnh sửa
        if (EditingSkus.Count > 1)
        {
            var defaultSku = EditingSkus.FirstOrDefault(s => 
                (s.Color == "Quy cách chuẩn" && s.Size == "Nguyên bản") ||
                (s.Color == "Mặc định" && s.Size == "Tiêu chuẩn"));
            if (defaultSku != null) EditingSkus.Remove(defaultSku);
        }
    }

    /// <summary>
    /// Lưu sản phẩm (Tạo mới hoặc Cập nhật)
    /// </summary>
    [RelayCommand]
    public async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(EditProductName))
        {
            ShowError("Vui lòng nhập tên sản phẩm.");
            return;
        }

        if (EditSelectedCategory == null || EditSelectedCategory.Id == Guid.Empty)
        {
            ShowError("Vui lòng chọn danh mục cho sản phẩm.");
            return;
        }

        if (EditingSkus.Count == 0)
        {
            ShowError("Sản phẩm phải có ít nhất 1 biến thể SKU.");
            return;
        }

        // Kiểm tra trùng SkuCode hoặc Barcode
        var duplicateSku = EditingSkus.GroupBy(s => s.SkuCode.Trim().ToUpperInvariant())
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateSku != null)
        {
            ShowError($"Mã SKU '{duplicateSku.Key}' bị trùng lặp.");
            return;
        }

        var duplicateBarcode = EditingSkus.GroupBy(s => s.Barcode.Trim())
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateBarcode != null)
        {
            ShowError($"Mã vạch '{duplicateBarcode.Key}' bị trùng lặp.");
            return;
        }

        IsLoading = true;

        try
        {
            if (!IsEditMode)
            {
                // TẠO MỚI SẢN PHẨM
                var createSkuRequests = EditingSkus.Select(s => new CreateSkuRequest(
                    SkuCode: s.SkuCode.Trim().ToUpperInvariant(),
                    Barcode: s.Barcode.Trim(),
                    CostPrice: s.CostPrice,
                    SellPrice: s.SellPrice,
                    TaxRate: s.TaxRate,
                    Attributes: s.BuildAttributesJson()
                )).ToList();

                var createReq = new CreateProductRequest(
                    Name: EditProductName.Trim(),
                    CategoryId: EditSelectedCategory.Id,
                    BaseUnit: EditBaseUnit,
                    Description: string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Brand: string.IsNullOrWhiteSpace(EditBrand) ? null : EditBrand.Trim(),
                    ImageUrl: string.IsNullOrWhiteSpace(EditImageUrl) ? null : EditImageUrl.Trim(),
                    Skus: createSkuRequests
                );

                var res = await _productApi.CreateProductAsync(createReq);
                if (res != null && res.Success)
                {
                    ShowStatus($"Tạo sản phẩm '{EditProductName}' thành công!");
                    CloseProductEditModal();
                    _queryCache.InvalidateByPrefix("products");
                    _queryCache.InvalidateByPrefix("kpi");
                    _queryCache.InvalidateByPrefix("pos-catalog");
                    await LoadProductsAsync();
                    await LoadKpiSummaryAsync();
                }
                else
                {
                    ShowError(res?.Error?.Message ?? "Không thể tạo sản phẩm. Vui lòng thử lại.");
                }
            }
            else
            {
                // CẬP NHẬT SẢN PHẨM HIỆN TẠI
                var updateReq = new UpdateProductRequest(
                    Name: EditProductName.Trim(),
                    CategoryId: EditSelectedCategory.Id,
                    BaseUnit: EditBaseUnit,
                    Description: string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Brand: string.IsNullOrWhiteSpace(EditBrand) ? null : EditBrand.Trim(),
                    ImageUrl: string.IsNullOrWhiteSpace(EditImageUrl) ? null : EditImageUrl.Trim(),
                    Status: EditStatus
                );

                var res = await _productApi.UpdateProductAsync(EditProductId, updateReq);
                if (res != null && res.Success)
                {
                    // Đồng bộ SKU mới hoặc đã sửa
                    foreach (var sku in EditingSkus)
                    {
                        if (sku.Id == Guid.Empty)
                        {
                            // Tạo SKU mới thêm vào sản phẩm
                            var createSkuReq = new CreateSkuRequest(
                                SkuCode: sku.SkuCode.Trim().ToUpperInvariant(),
                                Barcode: sku.Barcode.Trim(),
                                CostPrice: sku.CostPrice,
                                SellPrice: sku.SellPrice,
                                TaxRate: sku.TaxRate,
                                Attributes: sku.BuildAttributesJson(),
                                ProductId: EditProductId
                            );
                            await _productApi.CreateSkuAsync(createSkuReq);
                        }
                        else
                        {
                            // Cập nhật SKU
                            var updateSkuReq = new UpdateSkuRequest(
                                SkuCode: sku.SkuCode.Trim().ToUpperInvariant(),
                                Barcode: sku.Barcode.Trim(),
                                CostPrice: sku.CostPrice,
                                SellPrice: sku.SellPrice,
                                TaxRate: sku.TaxRate,
                                IsActive: sku.IsActive,
                                Attributes: sku.BuildAttributesJson()
                            );
                            await _productApi.UpdateSkuAsync(sku.Id, updateSkuReq);
                        }
                    }

                    ShowSuccess($"Cập nhật sản phẩm '{EditProductName}' thành công!");
                    CloseProductEditModal();
                    _queryCache.InvalidateByPrefix("products");
                    _queryCache.InvalidateByPrefix("kpi");
                    _queryCache.InvalidateByPrefix("pos-catalog");
                    await LoadProductsAsync();
                    await LoadKpiSummaryAsync();
                }
                else
                {
                    ShowError(res?.Error?.Message ?? "Không thể cập nhật sản phẩm.");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GeneratePrefixFromProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "SP";
        var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            var clean = ToSlug(words[0]).ToUpperInvariant();
            return clean.Length > 4 ? clean[..4] : clean;
        }

        var sb = new StringBuilder();
        foreach (var w in words)
        {
            var slug = ToSlug(w);
            if (slug.Length > 0) sb.Append(slug[0]);
        }
        return sb.ToString().ToUpperInvariant();
    }

    private static string GenerateRandomBarcode()
    {
        // Sinh barcode chuẩn 12 số (bắt đầu bằng 893 mã quốc gia VN)
        var random = RandomNumberGenerator.GetInt32(100000000, 999999999);
        return $"893{random}";
    }

    private static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var text = input.Trim().ToLowerInvariant();
        text = Regex.Replace(text, @"[áàảãạăắằẳẵặâấầẩẫậ]", "a");
        text = Regex.Replace(text, @"[éèẻẽẹêếềểễệ]", "e");
        text = Regex.Replace(text, @"[íìỉĩị]", "i");
        text = Regex.Replace(text, @"[óòỏõọôốồổỗộơớờởỡợ]", "o");
        text = Regex.Replace(text, @"[úùủũụưứừửữự]", "u");
        text = Regex.Replace(text, @"[ýỳỷỹỵ]", "y");
        text = Regex.Replace(text, @"đ", "d");
        text = Regex.Replace(text, @"[^a-z0-9]", "");
        return text;
    }
}
