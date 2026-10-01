using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Import;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

public class BulkImportProductsCommandHandler : ICommandHandler<BulkImportProductsCommand, BulkImportResultDto>
{
    private static readonly decimal[] AllowedTaxRates = [0m, 5m, 8m, 10m];

    private readonly IExcelImportParser _parser;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public BulkImportProductsCommandHandler(
        IExcelImportParser parser,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        ISkuRepository skuRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _parser = parser;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _skuRepository = skuRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportProductsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
            return Result<BulkImportResultDto>.Failure(ProductErrors.StoreRequired);

        var storeId = _currentUser.StoreId.Value;

        // ── 1. Parse Excel ────────────────────────────────────────────────────
        ExcelParseResult parseResult;
        try
        {
            parseResult = _parser.Parse(request.FileContent);
        }
        catch (Exception ex)
        {
            return Result<BulkImportResultDto>.Failure(
                new Error(ErrorType.Validation, "BulkImport.ParseError",
                    $"Không thể đọc file Excel: {ex.Message}"));
        }

        var errors = new List<string>(parseResult.ParseErrors);
        var totalRows = parseResult.Rows.Count;

        if (totalRows == 0 && errors.Count > 0)
        {
            return new BulkImportResultDto(0, 0, 0, errors.AsReadOnly());
        }

        // ── 2. Pre-load categories for the store (name → entity map) ─────────
        var allCategories = await _categoryRepository.GetAllByStoreIdAsync(storeId, cancellationToken);
        var categoryMap = allCategories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // ── 3. Track in-memory uniqueness across this batch ───────────────────
        var batchSkuCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int successCount = 0;

        // ── 4. Process each row ───────────────────────────────────────────────
        foreach (var row in parseResult.Rows)
        {
            var rowErrors = new List<string>();
            var prefix = $"Dòng {row.RowNumber}:";

            // --- Validate required text fields ---
            if (string.IsNullOrWhiteSpace(row.Name))
                rowErrors.Add($"{prefix} Tên sản phẩm (cột Name) không được để trống.");

            if (string.IsNullOrWhiteSpace(row.BaseUnit))
                rowErrors.Add($"{prefix} Đơn vị cơ bản (cột BaseUnit) không được để trống.");

            if (string.IsNullOrWhiteSpace(row.SkuCode))
                rowErrors.Add($"{prefix} Mã SKU (cột SkuCode) không được để trống.");

            if (string.IsNullOrWhiteSpace(row.Barcode))
                rowErrors.Add($"{prefix} Mã vạch (cột Barcode) không được để trống.");

            // --- Validate numeric fields ---
            if (row.SellPrice is null || row.SellPrice < 0)
                rowErrors.Add($"{prefix} Giá bán (cột SellPrice) phải >= 0.");

            if (row.CostPrice is null || row.CostPrice < 0)
                rowErrors.Add($"{prefix} Giá vốn (cột CostPrice) phải >= 0.");

            var taxRate = row.TaxRate ?? 0m;
            if (!AllowedTaxRates.Contains(taxRate))
                rowErrors.Add($"{prefix} Thuế suất VAT phải là 0, 5, 8 hoặc 10 (hiện tại: {taxRate}).");

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

            // --- In-batch duplicate check ---
            if (!string.IsNullOrWhiteSpace(row.SkuCode))
            {
                if (!batchSkuCodes.Add(row.SkuCode!))
                    rowErrors.Add($"{prefix} Mã SKU '{row.SkuCode}' bị trùng trong file.");
            }

            if (!string.IsNullOrWhiteSpace(row.Barcode))
            {
                if (!batchBarcodes.Add(row.Barcode!))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' bị trùng trong file.");
            }

            // --- DB uniqueness check (only if no errors so far) ---
            if (rowErrors.Count == 0)
            {
                if (!await _skuRepository.IsSkuCodeUniqueAsync(row.SkuCode!, storeId, null, cancellationToken))
                    rowErrors.Add($"{prefix} Mã SKU '{row.SkuCode}' đã tồn tại trong cửa hàng.");

                if (!await _skuRepository.IsBarcodeUniqueAsync(row.Barcode!, storeId, null, cancellationToken))
                    rowErrors.Add($"{prefix} Mã vạch '{row.Barcode}' đã tồn tại trong cửa hàng.");
            }

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                // Remove from in-memory sets so errored rows don't pollute future duplicate checks
                batchSkuCodes.Remove(row.SkuCode ?? string.Empty);
                batchBarcodes.Remove(row.Barcode ?? string.Empty);
                continue;
            }

            // ── 5. Persist (product + sku as atomic unit) ────────────────────
            try
            {
                var product = new Product(
                    storeId,
                    category!.Id,
                    row.Name!,
                    row.BaseUnit!,
                    brand: row.Brand
                );
                await _productRepository.AddAsync(product, cancellationToken);

                var sku = new Sku(
                    product.Id,
                    storeId,
                    row.SkuCode!,
                    row.Barcode!,
                    row.SellPrice!.Value,
                    row.CostPrice!.Value,
                    taxRate,
                    isActive: true
                );
                await _skuRepository.AddAsync(sku, cancellationToken);

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Dòng {row.RowNumber}: Lỗi khi lưu — {ex.Message}");
                batchSkuCodes.Remove(row.SkuCode!);
                batchBarcodes.Remove(row.Barcode!);
            }
        }

        // ── 6. Commit all successful rows in one transaction ─────────────────
        if (successCount > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkImportResultDto(
            TotalRows: totalRows,
            SuccessCount: successCount,
            FailureCount: totalRows - successCount,
            Errors: errors.AsReadOnly()
        );
    }
}
