using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Excel;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;
using POS.Domain.Inventory.StockIn;
using POS.Domain.Products;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.ImportStockInVoucher;

/// <summary>
/// Handler nhập kho hàng loạt từ file Excel (bảng kê giao hàng của Nhà Cung Cấp).
/// [LUỒNG 2]:
/// 1. Xác thực nhân viên, cửa hàng và nhà cung cấp.
/// 2. Parse file Excel bằng IExcelReader generic engine.
/// 3. Batch pre-fetch SKU theo cả SkuCode và Barcode trong 1-2 query duy nhất O(1).
/// 4. Tạo thực thể StockInVoucher và các dòng StockInVoucherItem.
/// 5. Nếu AutoComplete == true:
///    - Thực thi trong ExecuteSerializableAsync.
///    - Chuyển trạng thái phiếu sang Completed.
///    - Cập nhật StockEntry (QtyOnHand + AverageCost theo chuẩn Weighted Average Cost).
///    - Tạo mới hoặc cộng dồn StockBatch (phục vụ FEFO/FIFO khi bán hàng).
///    - Tạo StockTransaction.CreateStockIn ghi sổ cái kiểm toán bất biến.
/// </summary>
public class ImportStockInVoucherCommandHandler : ICommandHandler<ImportStockInVoucherCommand, ImportStockInVoucherResultDto>
{
    private readonly IExcelReader               _excelReader;
    private readonly IStockInVoucherRepository  _stockInVoucherRepository;
    private readonly ISkuRepository             _skuRepository;
    private readonly ISupplierRepository        _supplierRepository;
    private readonly IStockEntryRepository      _stockEntryRepository;
    private readonly IStockBatchRepository      _stockBatchRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IEmployeeRepository        _employeeRepository;
    private readonly IUnitOfWork                _unitOfWork;
    private readonly IStoreAccessPolicy         _storeAccessPolicy;
    private readonly ICurrentUser               _currentUser;

    public ImportStockInVoucherCommandHandler(
        IExcelReader               excelReader,
        IStockInVoucherRepository  stockInVoucherRepository,
        ISkuRepository             skuRepository,
        ISupplierRepository        supplierRepository,
        IStockEntryRepository      stockEntryRepository,
        IStockBatchRepository      stockBatchRepository,
        IStockTransactionRepository stockTransactionRepository,
        IEmployeeRepository        employeeRepository,
        IUnitOfWork                unitOfWork,
        IStoreAccessPolicy         storeAccessPolicy,
        ICurrentUser               currentUser)
    {
        _excelReader               = excelReader;
        _stockInVoucherRepository  = stockInVoucherRepository;
        _skuRepository             = skuRepository;
        _supplierRepository        = supplierRepository;
        _stockEntryRepository      = stockEntryRepository;
        _stockBatchRepository      = stockBatchRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _employeeRepository        = employeeRepository;
        _unitOfWork                = unitOfWork;
        _storeAccessPolicy         = storeAccessPolicy;
        _currentUser               = currentUser;
    }

    public async Task<Result<ImportStockInVoucherResultDto>> Handle(
        ImportStockInVoucherCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Xác thực nhân viên & quyền truy cập cửa hàng ─────────────────
        if (_currentUser.EmployeeId is null)
            return StockInErrors.Unauthorized;

        var employee = await _employeeRepository.GetByIdAsync(_currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockInErrors.Unauthorized;

        var storeContext = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeContext.IsFailure) return storeContext.Error;
        var storeId = storeContext.Value;

        // Chỉ ChainOwner hoặc nhân viên đúng cửa hàng mới được tạo phiếu nhập
        if (!employee.IsChainOwner && employee.StoreId != storeId)
            return StockInErrors.AccessDenied;

        // ── 2. Xác thực Nhà cung cấp ───────────────────────────────────────
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            return StockInErrors.SupplierNotFound;

        // ── 3. Parse Excel bằng IExcelReader generic engine ──────────────────
        ExcelReadResult<StockInImportRowDto> readResult;
        try
        {
            readResult = _excelReader.Read<StockInImportRowDto>(request.FileContent);
        }
        catch (Exception ex)
        {
            return Result<ImportStockInVoucherResultDto>.Failure(
                new Error(ErrorType.Validation, "StockInImport.ParseError",
                    $"Không thể đọc file Excel: {ex.Message}"));
        }

        var errors = new List<string>(readResult.GlobalErrors);
        foreach (var rowResult in readResult.Rows.Where(r => r.Errors.Count > 0))
            foreach (var err in rowResult.Errors)
                errors.Add($"Dòng {rowResult.RowNumber}: {err}");

        var validRowResults = readResult.Rows.Where(r => r.Errors.Count == 0).ToList();
        var totalRows = readResult.Rows.Count;

        if (totalRows == 0 && errors.Count > 0)
        {
            return new ImportStockInVoucherResultDto(
                VoucherId:    null,
                VoucherCode:  null,
                TotalRows:    0,
                SuccessCount: 0,
                FailureCount: 0,
                TotalAmount:  0,
                Errors:       errors.AsReadOnly());
        }

        // ── 4. Batch Pre-fetch SKU theo cả SkuCode và Barcode (O(1)) ────────
        var allSkuCodes = validRowResults
            .Select(r => r.Item.SkuCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var skuMapByCode = await _skuRepository.GetBySkuCodesAsync(allSkuCodes, storeId, cancellationToken);

        var allBarcodes = validRowResults
            .Select(r => r.Item.Barcode)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var skuMapByBarcode = await _skuRepository.GetByBarcodesAsync(allBarcodes, storeId, cancellationToken);

        // ── 5. Tạo thực thể phiếu nhập kho StockInVoucher ───────────────────
        var note = !string.IsNullOrWhiteSpace(request.Note)
            ? request.Note
            : $"Nhập kho Excel từ NCC {supplier.Name} ({request.FileName ?? "bảng kê"})";

        var voucher = StockInVoucher.Create(storeId, supplier.Id, employee.Id, note);
        int successCount = 0;

        foreach (var rowResult in validRowResults)
        {
            var row = rowResult.Item;
            var prefix = $"Dòng {rowResult.RowNumber}:";

            // Tìm SKU: ưu tiên theo SkuCode, fallback sang Barcode
            Sku? sku = null;
            if (!string.IsNullOrWhiteSpace(row.SkuCode) && skuMapByCode.TryGetValue(row.SkuCode!.Trim(), out var s1))
            {
                sku = s1;
            }
            else if (!string.IsNullOrWhiteSpace(row.Barcode) && skuMapByBarcode.TryGetValue(row.Barcode!.Trim(), out var s2))
            {
                sku = s2;
            }

            if (sku is null)
            {
                var identifier = !string.IsNullOrWhiteSpace(row.SkuCode)
                    ? $"Mã SKU '{row.SkuCode}'"
                    : $"Mã vạch '{row.Barcode}'";
                errors.Add($"{prefix} Không tìm thấy sản phẩm với {identifier} trong cửa hàng.");
                continue;
            }

            if (row.Qty is null || row.Qty <= 0)
            {
                errors.Add($"{prefix} Số lượng nhập phải lớn hơn 0.");
                continue;
            }

            if (row.UnitPrice is null || row.UnitPrice < 0)
            {
                errors.Add($"{prefix} Đơn giá nhập không được âm.");
                continue;
            }

            var addItemResult = voucher.AddItem(
                skuId:      sku.Id,
                qty:        row.Qty.Value,
                unitPrice:  row.UnitPrice.Value,
                batchNo:    row.BatchNo?.Trim(),
                expiryDate: row.ExpiryDate);

            if (addItemResult.IsFailure)
            {
                errors.Add($"{prefix} {addItemResult.Error.Message}");
                continue;
            }

            successCount++;
        }

        if (successCount == 0)
        {
            return new ImportStockInVoucherResultDto(
                VoucherId:    null,
                VoucherCode:  null,
                TotalRows:    totalRows,
                SuccessCount: 0,
                FailureCount: totalRows,
                TotalAmount:  0,
                Errors:       errors.AsReadOnly());
        }

        // Lưu Voucher vào DB
        await _stockInVoucherRepository.AddAsync(voucher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 6. Tự động hoàn thành phiếu & tăng tồn kho (nếu AutoComplete) ──
        if (request.AutoComplete)
        {
            var completeResult = await _unitOfWork.ExecuteSerializableAsync<bool>(async ct =>
            {
                var freshVoucher = await _stockInVoucherRepository.GetByIdWithItemsAsync(voucher.Id, ct);
                if (freshVoucher is null) return StockInErrors.VoucherNotFound;

                var compResult = freshVoucher.Complete();
                if (compResult.IsFailure) return compResult.Error;

                var skuIds = freshVoucher.Items.Select(i => i.SkuId).Distinct().ToList();
                var stockEntries = await _stockEntryRepository.GetBySkuIdsAsync(skuIds, freshVoucher.StoreId, ct);

                foreach (var item in freshVoucher.Items)
                {
                    // a. Cập nhật tổng tồn & tính lại Weighted Average Cost
                    if (!stockEntries.TryGetValue(item.SkuId, out var entry))
                    {
                        entry = new StockEntry(freshVoucher.StoreId, item.SkuId, 0m);
                        await _stockEntryRepository.AddAsync(entry, ct);
                        stockEntries[item.SkuId] = entry;
                    }

                    entry.IncreaseStock(item.Qty, item.UnitPrice);

                    // b. Ghi sổ cái kiểm toán bất biến
                    var tx = StockTransaction.CreateStockIn(
                        storeId:          freshVoucher.StoreId,
                        skuId:            item.SkuId,
                        qty:              item.Qty,
                        createdBy:        employee.Id,
                        stockInVoucherId: freshVoucher.Id,
                        unitCost:         item.UnitPrice,
                        note:             $"Nhập kho Excel phiếu {freshVoucher.Id.ToString()[..8].ToUpperInvariant()}");

                    await _stockTransactionRepository.AddAsync(tx, ct);

                    // c. Bổ sung hoặc cộng dồn lô hàng (phục vụ FEFO/FIFO)
                    var batchNo = !string.IsNullOrWhiteSpace(item.BatchNo)
                        ? item.BatchNo.Trim()
                        : $"NK-{DateTime.UtcNow:yyyyMMdd}-{item.SkuId.ToString()[..4].ToUpperInvariant()}";

                    var existingBatch = await _stockBatchRepository.GetByBatchNoAsync(
                        freshVoucher.StoreId, item.SkuId, batchNo, ct);

                    if (existingBatch is null)
                    {
                        var newBatch = new StockBatch(
                            storeId:    freshVoucher.StoreId,
                            skuId:      item.SkuId,
                            batchNo:    batchNo,
                            qty:        item.Qty,
                            expiryDate: item.ExpiryDate);
                        await _stockBatchRepository.AddAsync(newBatch, ct);
                    }
                    else
                    {
                        existingBatch.AddQty(item.Qty);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
                return Result<bool>.Success(true);
            }, cancellationToken);

            if (completeResult.IsFailure)
            {
                errors.Add($"Lưu phiếu nháp thành công nhưng lỗi khi hoàn thành tồn kho: {completeResult.Error.Message}");
            }
        }

        var voucherCode = $"NK-{voucher.Id.ToString()[..8].ToUpperInvariant()}";

        return new ImportStockInVoucherResultDto(
            VoucherId:    voucher.Id,
            VoucherCode:  voucherCode,
            TotalRows:    totalRows,
            SuccessCount: successCount,
            FailureCount: totalRows - successCount,
            TotalAmount:  voucher.TotalAmount,
            Errors:       errors.AsReadOnly());
    }
}
