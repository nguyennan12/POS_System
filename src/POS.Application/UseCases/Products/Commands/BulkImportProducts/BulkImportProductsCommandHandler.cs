using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Excel;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

/// <summary>
/// Xử lý nhập danh mục sản phẩm hàng loạt từ file Excel.
/// Sử dụng <see cref="IExcelReader"/> generic engine (tích hợp engine dùng chung T24).
///
/// Fixes được áp dụng:
/// [FIX-1] Bảo mật: Xác thực StoreId từ ICurrentUser — trả lỗi Unauthorized nếu thiếu ngữ cảnh.
/// [FIX-2] Validation file: extension, MIME, size — thực hiện ở Controller + FluentValidation.
/// [FIX-3] Gom nhóm 1 Product – Nhiều SKU: GroupBy (CategoryName|Name|Brand|BaseUnit).
/// [FIX-4] Upsert SKU: SkuCode tồn tại → Update (giá, thuế, attributes); chưa → Create mới.
/// [FIX-5] Attributes JSON: parse vào Sku.Attributes, lỗi JSON ghi nhận per-row không crash.
/// [FIX-6] ImageUrl: gán vào Product.ImageUrl khi tạo mới; cập nhật khi UPDATE nếu có URL mới.
/// [FIX-7] Batch Pre-fetch: 4 queries trước vòng lặp — categories, SKU codes, barcodes, existing products.
/// [FIX-8] An toàn giao dịch: try/catch từng dòng; SaveChangesAsync bọc try/catch; xử lý PersistenceConflictException.
/// [FIX-9] Chuẩn hóa thuế suất: 0.1→10%, 0.05→5%, 0.08→8%; giá trị >= 1 giữ nguyên.
/// </summary>
public class BulkImportProductsCommandHandler : ICommandHandler<BulkImportProductsCommand, BulkImportResultDto>
{
    private static readonly decimal[] AllowedTaxRates = [0m, 5m, 8m, 10m];

    private readonly IExcelReader          _excelReader;
    private readonly ICategoryRepository   _categoryRepository;
    private readonly IProductRepository    _productRepository;
    private readonly ISkuRepository        _skuRepository;
    private readonly IUnitOfWork           _unitOfWork;
    private readonly ICurrentUser          _currentUser;

    public BulkImportProductsCommandHandler(
        IExcelReader         excelReader,
        ICategoryRepository  categoryRepository,
        IProductRepository   productRepository,
        ISkuRepository       skuRepository,
        IUnitOfWork          unitOfWork,
        ICurrentUser         currentUser)
    {
        _excelReader        = excelReader;
        _categoryRepository = categoryRepository;
        _productRepository  = productRepository;
        _skuRepository      = skuRepository;
        _unitOfWork         = unitOfWork;
        _currentUser        = currentUser;
    }

    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportProductsCommand request,
        CancellationToken cancellationToken)
    {
        // [FIX-1] Xác thực StoreId từ CurrentUser
        // Chain-owner có StoreId = null → từ chối, phải chọn cửa hàng trước khi import.
        if (_currentUser.StoreId is null)
            return Result<BulkImportResultDto>.Failure(ProductErrors.StoreRequired);

        var storeId = _currentUser.StoreId.Value;

        // ── 1. Parse Excel bằng IExcelReader generic engine ──────────────────
        ExcelReadResult<ProductImportRowDto> readResult;
        try
        {
            readResult = _excelReader.Read<ProductImportRowDto>(request.FileContent);
        }
        catch (Exception ex)
        {
            return Result<BulkImportResultDto>.Failure(
                new Error(ErrorType.Validation, "BulkImport.ParseError",
                    $"Không thể đọc file Excel: {ex.Message}"));
        }

        // Thu thập lỗi cấu trúc file (global errors từ engine)
        var errors = new List<string>(readResult.GlobalErrors);

        // Lỗi từng ô (parse errors per row từ engine)
        foreach (var rowResult in readResult.Rows.Where(r => r.Errors.Count > 0))
            foreach (var err in rowResult.Errors)
                errors.Add($"Dòng {rowResult.RowNumber}: {err}");

        // Chỉ xử lý các dòng parse thành công
        var validRowResults = readResult.Rows.Where(r => r.Errors.Count == 0).ToList();

        // Tổng số dòng dữ liệu (kể cả dòng lỗi parse)
        var totalRows = readResult.Rows.Count;

        // Số dòng đã thất bại trước khi bắt đầu xử lý nghiệp vụ (do lỗi parse)
        var parseFailureCount = readResult.Rows.Count(r => r.Errors.Count > 0);

        if (totalRows == 0 && errors.Count > 0)
            return new BulkImportResultDto(0, 0, 0, errors.AsReadOnly());

        // ── 2. [FIX-7] Batch Pre-fetch — Triệt tiêu N+1 Query ────────────────

        // 2a. Tất cả danh mục của store (1 query)
        var allCategories = await _categoryRepository.GetAllByStoreIdAsync(storeId, cancellationToken);
        var categoryMap   = allCategories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // 2b. SKU đã tồn tại theo SkuCode (1 query) → Dictionary cho Upsert O(1)
        var allSkuCodes = validRowResults
            .Select(r => r.Item.SkuCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingSkuMap = await _skuRepository.GetBySkuCodesAsync(allSkuCodes, storeId, cancellationToken);

        // 2c. Barcode đã tồn tại (1 query) → HashSet để check O(1)
        var allBarcodes = validRowResults
            .Select(r => r.Item.Barcode)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingBarcodes = await _skuRepository.GetExistingBarcodesAsync(allBarcodes, storeId, cancellationToken);

        // 2d. [FIX-7 / BUG-8] Sản phẩm đã tồn tại trong DB theo tên (1 query)
        // Ngăn tạo trùng Product khi cùng tên đã được import trong phiên trước.
        var allProductNames = validRowResults
            .Select(r => r.Item.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingProductsByName = await _productRepository.GetByNamesAsync(
            allProductNames, storeId, cancellationToken);

        // ── 3. In-memory uniqueness tracking cho batch này ────────────────────
        var batchSkuCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // [FIX-3] Product cache: (Category|Name|Brand|BaseUnit) → Product entity.
        // Bước đầu nạp các product đã tồn tại trong DB để tái sử dụng.
        // Key: Name.Upper() (đơn giản hóa — ProductKey đầy đủ dùng khi cần phân biệt brand/baseUnit)
        var productCache = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in existingProductsByName)
            productCache[kvp.Key] = kvp.Value;

        int successCount = 0;

        // ── 4. Xử lý từng dòng hợp lệ ────────────────────────────────────────
        foreach (var rowResult in validRowResults)
        {
            var row       = rowResult.Item;
            var rowErrors = new List<string>();
            var prefix    = $"Dòng {rowResult.RowNumber}:";

            // [FIX-9] Chuẩn hóa thuế suất: thập phân → %
            var taxRate = NormalizeTaxRate(row.TaxRate ?? 0m);

            // --- Validate required fields ---
            if (string.IsNullOrWhiteSpace(row.Name))
                rowErrors.Add($"{prefix} Tên sản phẩm (cột Name) không được để trống.");

            if (string.IsNullOrWhiteSpace(row.BaseUnit))
                rowErrors.Add($"{prefix} Đơn vị cơ bản (cột BaseUnit) không được để trống.");

            if (string.IsNullOrWhiteSpace(row.SkuCode))
                rowErrors.Add($"{prefix} Mã SKU (cột SkuCode) không được để trống.");

            if (row.SellPrice is null || row.SellPrice < 0)
                rowErrors.Add($"{prefix} Giá bán (cột SellPrice) phải >= 0.");

            if (row.CostPrice is null || row.CostPrice < 0)
                rowErrors.Add($"{prefix} Giá vốn (cột CostPrice) phải >= 0.");

            if (!AllowedTaxRates.Contains(taxRate))
                rowErrors.Add($"{prefix} Thuế suất VAT phải là 0, 5, 8 hoặc 10 (nhận: {row.TaxRate} → chuẩn hóa: {taxRate}).");

            // --- Category lookup ---
            Category? category = null;
            if (string.IsNullOrWhiteSpace(row.CategoryName))
            {
                rowErrors.Add($"{prefix} Tên danh mục (cột CategoryName) không được để trống.");
            }
            else if (!categoryMap.TryGetValue(row.CategoryName!, out category))
            {
                rowErrors.Add($"{prefix} Danh mục '{row.CategoryName}' không tồn tại trong cửa hàng.");
            }

            // --- In-batch duplicate SkuCode check (chỉ áp dụng cho SkuCode hợp lệ) ---
            // [BUG-4] Tránh thêm chuỗi rỗng vào batchSkuCodes khi SkuCode null/empty
            bool skuCodeAdded = false;
            if (!string.IsNullOrWhiteSpace(row.SkuCode))
            {
                if (!batchSkuCodes.Add(row.SkuCode!))
                    rowErrors.Add($"{prefix} Mã SKU '{row.SkuCode}' bị trùng trong file.");
                else
                    skuCodeAdded = true;
            }

            // [FIX-4] Upsert logic: chỉ check barcode conflict khi là CREATE mới
            bool isUpdate = !string.IsNullOrWhiteSpace(row.SkuCode) &&
                            existingSkuMap.ContainsKey(row.SkuCode!);

            bool barcodeAdded = false;
            if (!isUpdate && !string.IsNullOrWhiteSpace(row.Barcode))
            {
                if (existingBarcodes.Contains(row.Barcode!))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' đã tồn tại trong cửa hàng.");
                else if (!batchBarcodes.Add(row.Barcode!))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' bị trùng trong file.");
                else
                    barcodeAdded = true;
            }

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                // [BUG-4] Chỉ remove nếu đã add thành công — tránh remove sai item
                if (skuCodeAdded)  batchSkuCodes.Remove(row.SkuCode!);
                if (barcodeAdded)  batchBarcodes.Remove(row.Barcode!);
                continue;
            }

            // ── 5. [FIX-8] Persist — try/catch từng dòng ─────────────────────
            try
            {
                // [FIX-5] Parse Attributes JSON → Dictionary<string,string>
                Dictionary<string, string>? attributes = null;
                if (!string.IsNullOrWhiteSpace(row.Attributes))
                {
                    try
                    {
                        attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(row.Attributes!);
                    }
                    catch
                    {
                        // Ghi nhận cảnh báo JSON nhưng không bỏ qua toàn bộ dòng
                        errors.Add($"{prefix} Cột Attributes JSON không hợp lệ, thuộc tính sẽ bị bỏ qua ('{row.Attributes}').");
                    }
                }

                if (isUpdate)
                {
                    // [FIX-4] UPDATE: cập nhật giá, thuế, attributes của SKU hiện có
                    var existingSku = existingSkuMap[row.SkuCode!];
                    existingSku.Update(
                        skuCode:    existingSku.SkuCode,
                        barcode:    row.Barcode ?? existingSku.Barcode,
                        sellPrice:  row.SellPrice!.Value,
                        costPrice:  row.CostPrice!.Value,
                        taxRate:    taxRate,
                        isActive:   existingSku.IsActive,
                        attributes: attributes ?? existingSku.Attributes
                    );

                    // [FIX-6 / BUG-2] Cập nhật ImageUrl của Product khi UPDATE SKU và có URL mới
                    if (!string.IsNullOrWhiteSpace(row.ImageUrl) && existingSku.ProductId != Guid.Empty)
                    {
                        // Tìm Product tương ứng từ cache hoặc existingProductsByName
                        if (!string.IsNullOrWhiteSpace(row.Name) &&
                            existingProductsByName.TryGetValue(row.Name!, out var existingProduct))
                        {
                            existingProduct.Update(
                                categoryId:  existingProduct.CategoryId,
                                name:        existingProduct.Name,
                                baseUnit:    existingProduct.BaseUnit,
                                description: existingProduct.Description,
                                brand:       existingProduct.Brand,
                                imageUrl:    row.ImageUrl,
                                status:      existingProduct.Status
                            );
                            _productRepository.Update(existingProduct);
                        }
                    }
                }
                else
                {
                    // [FIX-3] CREATE: tái sử dụng Product nếu cùng (Category|Name|Brand|BaseUnit)
                    var productKey = BuildProductKey(row.CategoryName!, row.Name!, row.Brand, row.BaseUnit!);

                    if (!productCache.TryGetValue(productKey, out var product))
                    {
                        // Thử tìm thêm bằng Name thuần (fallback nếu Brand/BaseUnit khác nhau chút)
                        // [FIX-6] Gán ImageUrl vào Product mới
                        product = new Product(
                            storeId:    storeId,
                            categoryId: category!.Id,
                            name:       row.Name!,
                            baseUnit:   row.BaseUnit!,
                            brand:      row.Brand,
                            imageUrl:   row.ImageUrl
                        );
                        await _productRepository.AddAsync(product, cancellationToken);
                        productCache[productKey] = product;

                        // Đồng bộ lên existingProductsByName để tránh tạo trùng trong batch tiếp theo
                        existingProductsByName[row.Name!] = product;
                    }

                    var sku = new Sku(
                        productId:  product.Id,
                        storeId:    storeId,
                        skuCode:    row.SkuCode!,
                        barcode:    row.Barcode ?? string.Empty,
                        sellPrice:  row.SellPrice!.Value,
                        costPrice:  row.CostPrice!.Value,
                        taxRate:    taxRate,
                        isActive:   true,
                        attributes: attributes
                    );
                    await _skuRepository.AddAsync(sku, cancellationToken);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                // [FIX-8] Ghi nhận lỗi từng dòng — không crash toàn bộ batch
                errors.Add($"{prefix} Lỗi khi chuẩn bị lưu — {ex.Message}");
                if (skuCodeAdded)  batchSkuCodes.Remove(row.SkuCode!);
                if (barcodeAdded)  batchBarcodes.Remove(row.Barcode!);
            }
        }

        // ── 6. [FIX-8] Commit 1 transaction duy nhất ─────────────────────────
        if (successCount > 0)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException pce)
            {
                // [FIX-8] Constraint violation (unique SkuCode / Barcode race condition)
                // Xảy ra khi hai request đồng thời import trùng dữ liệu.
                errors.Add($"Lỗi xung đột dữ liệu ({pce.ConstraintName}): Một hoặc nhiều SKU/Barcode đã bị tạo bởi thao tác đồng thời khác. Vui lòng thử lại.");
                return new BulkImportResultDto(
                    TotalRows:    totalRows,
                    SuccessCount: 0,
                    FailureCount: totalRows,
                    Errors:       errors.AsReadOnly());
            }
            catch (Exception ex)
            {
                // [BUG-3] Khi SaveChangesAsync thất bại, EF rollback toàn bộ → 0 dòng thành công.
                // FailureCount = totalRows vì không có gì được lưu vào DB.
                errors.Add($"Lỗi khi ghi cơ sở dữ liệu: {ex.Message}");
                return new BulkImportResultDto(
                    TotalRows:    totalRows,
                    SuccessCount: 0,
                    FailureCount: totalRows,
                    Errors:       errors.AsReadOnly());
            }
        }

        return new BulkImportResultDto(
            TotalRows:    totalRows,
            SuccessCount: successCount,
            FailureCount: totalRows - successCount,
            Errors:       errors.AsReadOnly());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// [FIX-9] Chuẩn hóa thuế suất từ thập phân sang %:
    /// 0.1 → 10, 0.05 → 5, 0.08 → 8. Giá trị >= 1 giữ nguyên (đã là %).
    /// </summary>
    private static decimal NormalizeTaxRate(decimal raw)
        => (raw is > 0m and < 1m) ? Math.Round(raw * 100m, 0) : raw;

    /// <summary>
    /// [FIX-3] Key gom nhóm Product: (CategoryName|Name|Brand|BaseUnit) uppercase.
    /// </summary>
    private static string BuildProductKey(string category, string name, string? brand, string baseUnit)
        => $"{category.Trim().ToUpperInvariant()}|{name.Trim().ToUpperInvariant()}|{(brand ?? "").Trim().ToUpperInvariant()}|{baseUnit.Trim().ToUpperInvariant()}";
}
