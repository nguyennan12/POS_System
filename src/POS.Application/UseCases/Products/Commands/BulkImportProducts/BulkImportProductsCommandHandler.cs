using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Excel;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;
using POS.Domain.Products;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

/// <summary>
/// Handler nhập danh sách sản phẩm/SKU hàng loạt từ file Excel.
/// Tuân thủ quy trình xử lý đã được kiểm thử:
/// [FIX-1] Tách biệt parse lỗi và lưu DB: lỗi cấu trúc file chặn sớm; lỗi parse từng dòng thu thập per-row.
/// [FIX-2] Barcode tùy chọn: cột rỗng không ném lỗi; giá trị có được trim & check conflict O(1).
/// [FIX-3] Tái sử dụng Product: cùng (Category|Name|Brand|BaseUnit) chỉ tạo 1 Product, các dòng sau thêm SKU biến thể.
/// [FIX-4] Upsert theo SkuCode: SkuCode đã có trong DB cập nhật giá, thuế, attributes (không kiểm tra trùng Barcode với chính nó).
/// [FIX-5] Attributes JSON: parse vào Sku.Attributes, lỗi JSON ghi nhận per-row không crash.
/// [FIX-6] ImageUrl: gán vào Product.ImageUrl khi tạo mới; cập nhật khi UPDATE nếu có URL mới.
/// [FIX-7] Batch Pre-fetch: 4 queries trước vòng lặp — categories, SKU codes, barcodes, existing products.
/// [FIX-8] An toàn giao dịch: try/catch từng dòng; SaveChangesAsync bọc try/catch; xử lý PersistenceConflictException.
/// [FIX-9] Chuẩn hóa thuế suất: 0.1→10%, 0.05→5%, 0.08→8%; giá trị >= 1 giữ nguyên.
/// [FIX-10] Tự động tạo Category nếu chưa có (AutoCreateCategories = true).
/// [FIX-11] Khởi tạo tồn kho đầu kỳ (StockEntry, StockBatch, StockTransaction) khi tạo mới SKU.
/// </summary>
public class BulkImportProductsCommandHandler : ICommandHandler<BulkImportProductsCommand, BulkImportResultDto>
{
    private static readonly decimal[] AllowedTaxRates = [0m, 5m, 8m, 10m];

    private readonly IExcelReader               _excelReader;
    private readonly ICategoryRepository        _categoryRepository;
    private readonly IProductRepository         _productRepository;
    private readonly ISkuRepository             _skuRepository;
    private readonly IStockEntryRepository      _stockEntryRepository;
    private readonly IStockBatchRepository      _stockBatchRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IUnitOfWork                _unitOfWork;
    private readonly IStoreAccessPolicy         _storeAccessPolicy;
    private readonly ICurrentUser               _currentUser;

    public BulkImportProductsCommandHandler(
        IExcelReader               excelReader,
        ICategoryRepository        categoryRepository,
        IProductRepository         productRepository,
        ISkuRepository             skuRepository,
        IStockEntryRepository      stockEntryRepository,
        IStockBatchRepository      stockBatchRepository,
        IStockTransactionRepository stockTransactionRepository,
        IUnitOfWork                unitOfWork,
        IStoreAccessPolicy         storeAccessPolicy,
        ICurrentUser               currentUser)
    {
        _excelReader               = excelReader;
        _categoryRepository        = categoryRepository;
        _productRepository         = productRepository;
        _skuRepository             = skuRepository;
        _stockEntryRepository      = stockEntryRepository;
        _stockBatchRepository      = stockBatchRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _unitOfWork                = unitOfWork;
        _storeAccessPolicy         = storeAccessPolicy;
        _currentUser               = currentUser;
    }

    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportProductsCommand request,
        CancellationToken cancellationToken)
    {
        var storeContext = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeContext.IsFailure) return storeContext.Error;

        var storeId = storeContext.Value;

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

        if (totalRows == 0 && errors.Count > 0)
            return new BulkImportResultDto(0, 0, 0, errors.AsReadOnly());

        // ── 2. Batch Pre-fetch — Triệt tiêu N+1 Query ────────────────────────
        // 2a. Tất cả danh mục của store (1 query)
        var allCategories = await _categoryRepository.GetAllByStoreIdAsync(storeId, cancellationToken);
        var categoryMap   = allCategories
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // 2b. SKU đã tồn tại theo SkuCode (1 query) → Dictionary cho Upsert O(1)
        var allSkuCodes = validRowResults
            .Select(r => r.Item.SkuCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingSkuMap = await _skuRepository.GetBySkuCodesAsync(allSkuCodes, storeId, cancellationToken);
        var existingSkuIds = existingSkuMap.Values.Select(s => s.Id).ToList();
        var existingStockEntryMap = await _stockEntryRepository.GetBySkuIdsAsync(existingSkuIds, storeId, cancellationToken);

        // 2c. Barcode đã tồn tại (1 query) → HashSet để check O(1)
        var allBarcodes = validRowResults
            .Select(r => r.Item.Barcode)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingBarcodes = await _skuRepository.GetExistingBarcodesAsync(allBarcodes, storeId, cancellationToken);

        // 2d. Sản phẩm đã tồn tại trong DB theo tên (1 query)
        var allProductNames = validRowResults
            .Select(r => r.Item.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var existingProductsByName = await _productRepository.GetByNamesAsync(
            allProductNames, storeId, cancellationToken);

        // ── 3. In-memory uniqueness tracking cho batch này ────────────────────
        var batchSkuCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            // --- Category lookup / auto-create ---
            Category? category = null;
            if (string.IsNullOrWhiteSpace(row.CategoryName))
            {
                rowErrors.Add($"{prefix} Tên danh mục (cột CategoryName) không được để trống.");
            }
            else
            {
                var catName = row.CategoryName!.Trim();
                if (!categoryMap.TryGetValue(catName, out category))
                {
                    if (request.AutoCreateCategories)
                    {
                        category = Category.Create(storeId, catName);
                        await _categoryRepository.AddAsync(category, cancellationToken);
                        categoryMap[catName] = category;
                    }
                    else
                    {
                        rowErrors.Add($"{prefix} Danh mục '{row.CategoryName}' không tồn tại trong cửa hàng.");
                    }
                }
            }

            // --- In-batch duplicate SkuCode check ---
            bool skuCodeAdded = false;
            if (!string.IsNullOrWhiteSpace(row.SkuCode))
            {
                var trimmedSku = row.SkuCode!.Trim();
                if (!batchSkuCodes.Add(trimmedSku))
                    rowErrors.Add($"{prefix} Mã SKU '{row.SkuCode}' bị trùng trong file.");
                else
                    skuCodeAdded = true;
            }

            // --- Check barcode conflict ---
            bool isUpdate = !string.IsNullOrWhiteSpace(row.SkuCode) &&
                            existingSkuMap.ContainsKey(row.SkuCode!.Trim());

            bool barcodeAdded = false;
            if (!isUpdate && !string.IsNullOrWhiteSpace(row.Barcode))
            {
                var trimmedBarcode = row.Barcode!.Trim();
                if (existingBarcodes.Contains(trimmedBarcode))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' đã tồn tại trong cửa hàng.");
                else if (!batchBarcodes.Add(trimmedBarcode))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' bị trùng trong file.");
                else
                    barcodeAdded = true;
            }

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                if (skuCodeAdded) batchSkuCodes.Remove(row.SkuCode!.Trim());
                if (barcodeAdded) batchBarcodes.Remove(row.Barcode!.Trim());
                continue;
            }

            // ── 5. Persist — try/catch từng dòng ─────────────────────────────
            try
            {
                Dictionary<string, string>? attributes = null;
                if (!string.IsNullOrWhiteSpace(row.Attributes))
                {
                    try
                    {
                        attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(row.Attributes!);
                    }
                    catch
                    {
                        errors.Add($"{prefix} Cột Attributes JSON không hợp lệ, thuộc tính sẽ bị bỏ qua ('{row.Attributes}').");
                    }
                }

                if (isUpdate)
                {
                    var existingSku = existingSkuMap[row.SkuCode!.Trim()];
                    existingSku.Update(
                        skuCode:    existingSku.SkuCode,
                        barcode:    row.Barcode?.Trim() ?? existingSku.Barcode,
                        sellPrice:  row.SellPrice!.Value,
                        costPrice:  row.CostPrice!.Value,
                        taxRate:    taxRate,
                        isActive:   existingSku.IsActive,
                        attributes: attributes ?? existingSku.Attributes
                    );

                    if (!string.IsNullOrWhiteSpace(row.ImageUrl) && existingSku.ProductId != Guid.Empty)
                    {
                        if (!string.IsNullOrWhiteSpace(row.Name) &&
                            existingProductsByName.TryGetValue(row.Name!.Trim(), out var existingProduct))
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

                    // Nếu SKU đã tồn tại nhưng chưa có StockEntry, khởi tạo tồn đầu kỳ nếu có số lượng
                    var initialQty = row.InitialStock ?? request.DefaultInitialStock ?? 0m;
                    var minStock = row.MinStock ?? request.DefaultMinStock ?? 0m;
                    var costPrice = row.CostPrice!.Value;

                    if (!existingStockEntryMap.TryGetValue(existingSku.Id, out var existingEntry))
                    {
                        if (initialQty > 0)
                        {
                            var stockEntry = new StockEntry(storeId, existingSku.Id, 0m, minStock: minStock);
                            stockEntry.IncreaseStock(initialQty, costPrice);
                            await _stockEntryRepository.AddAsync(stockEntry, cancellationToken);
                            existingStockEntryMap[existingSku.Id] = stockEntry;

                            var batchNo = !string.IsNullOrWhiteSpace(row.BatchNo)
                                ? row.BatchNo.Trim()
                                : $"INIT-{DateTime.UtcNow:yyyyMMdd}";
                            var stockBatch = new StockBatch(storeId, existingSku.Id, batchNo, initialQty, row.ExpiryDate);
                            await _stockBatchRepository.AddAsync(stockBatch, cancellationToken);

                            var createdBy = _currentUser.EmployeeId ?? Guid.Empty;
                            var tx = StockTransaction.CreateInitialStock(
                                storeId, existingSku.Id, initialQty, createdBy, costPrice, "Khởi tạo tồn kho đầu kỳ");
                            await _stockTransactionRepository.AddAsync(tx, cancellationToken);
                        }
                    }
                }
                else
                {
                    var productKey = BuildProductKey(row.CategoryName!, row.Name!, row.Brand, row.BaseUnit!);

                    if (!productCache.TryGetValue(productKey, out var product))
                    {
                        product = new Product(
                            storeId:    storeId,
                            categoryId: category!.Id,
                            name:       row.Name!.Trim(),
                            baseUnit:   row.BaseUnit!.Trim(),
                            brand:      row.Brand?.Trim(),
                            imageUrl:   row.ImageUrl?.Trim()
                        );
                        await _productRepository.AddAsync(product, cancellationToken);
                        productCache[productKey] = product;
                        existingProductsByName[row.Name!.Trim()] = product;
                    }

                    var sku = new Sku(
                        productId:  product.Id,
                        storeId:    storeId,
                        skuCode:    row.SkuCode!.Trim(),
                        barcode:    row.Barcode?.Trim() ?? string.Empty,
                        sellPrice:  row.SellPrice!.Value,
                        costPrice:  row.CostPrice!.Value,
                        taxRate:    taxRate,
                        isActive:   true,
                        attributes: attributes
                    );
                    await _skuRepository.AddAsync(sku, cancellationToken);

                    // ── Khởi tạo tồn kho đầu kỳ (StockEntry + StockBatch + StockTransaction) ──
                    var initialQty = row.InitialStock ?? request.DefaultInitialStock ?? 0m;
                    var minStock = row.MinStock ?? request.DefaultMinStock ?? 0m;
                    var costPrice = row.CostPrice!.Value;

                    if (initialQty > 0)
                    {
                        // 1. StockEntry: khởi tạo và tính giá vốn bình quân qua Domain method
                        var stockEntry = new StockEntry(storeId, sku.Id, 0m, minStock: minStock);
                        stockEntry.IncreaseStock(initialQty, costPrice);
                        await _stockEntryRepository.AddAsync(stockEntry, cancellationToken);

                        // 2. StockBatch: lô hàng phục vụ FEFO/FIFO
                        var batchNo = !string.IsNullOrWhiteSpace(row.BatchNo)
                            ? row.BatchNo.Trim()
                            : $"INIT-{DateTime.UtcNow:yyyyMMdd}";
                        var stockBatch = new StockBatch(
                            storeId:    storeId,
                            skuId:      sku.Id,
                            batchNo:    batchNo,
                            qty:        initialQty,
                            expiryDate: row.ExpiryDate);
                        await _stockBatchRepository.AddAsync(stockBatch, cancellationToken);

                        // 3. StockTransaction: sổ cái kiểm toán bất biến
                        var createdBy = _currentUser.EmployeeId ?? Guid.Empty;
                        var fileName = !string.IsNullOrWhiteSpace(request.FileName) ? request.FileName : "Excel";
                        var tx = StockTransaction.CreateInitialStock(
                            storeId:   storeId,
                            skuId:     sku.Id,
                            qty:       initialQty,
                            createdBy: createdBy,
                            unitCost:  costPrice,
                            note:      $"Khởi tạo tồn kho đầu kỳ từ file: {fileName}");
                        await _stockTransactionRepository.AddAsync(tx, cancellationToken);
                    }
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{prefix} Lỗi khi chuẩn bị lưu — {ex.Message}");
                if (skuCodeAdded) batchSkuCodes.Remove(row.SkuCode!.Trim());
                if (barcodeAdded) batchBarcodes.Remove(row.Barcode!.Trim());
            }
        }

        // ── 6. Commit transaction ────────────────────────────────────────────
        if (successCount > 0)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException pce)
            {
                errors.Add($"Lỗi xung đột dữ liệu ({pce.ConstraintName}): Một hoặc nhiều SKU/Barcode đã bị tạo bởi thao tác đồng thời khác. Vui lòng thử lại.");
                return new BulkImportResultDto(
                    TotalRows:    totalRows,
                    SuccessCount: 0,
                    FailureCount: totalRows,
                    Errors:       errors.AsReadOnly());
            }
            catch (Exception ex)
            {
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
    private static decimal NormalizeTaxRate(decimal raw)
        => (raw is > 0m and < 1m) ? Math.Round(raw * 100m, 0) : raw;

    private static string BuildProductKey(string category, string name, string? brand, string baseUnit)
        => $"{category.Trim().ToUpperInvariant()}|{name.Trim().ToUpperInvariant()}|{(brand ?? "").Trim().ToUpperInvariant()}|{baseUnit.Trim().ToUpperInvariant()}";
}
