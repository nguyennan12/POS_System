using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Products.Queries.GetProductById;
using POS.Application.UseCases.Products.Queries.GetProducts;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/products")]
public class ProductsController(
    ISender mediator,
    POS.Application.Abstractions.Excel.IExcelReader excelReader) : ControllerBase
{
    /// <summary>
    /// [P00] Danh mục sản phẩm tối ưu cho quầy bán hàng POS (kèm SKU, giá bán, tồn kho và mã vạch trong 1 request).
    /// </summary>
    [HttpGet("pos-catalog")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PosCatalogItemResponse>>>> GetPosCatalog(
        [FromQuery] PosCatalogFilterRequest request,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize   = Math.Min(200, Math.Max(1, request.PageSize));

        var result = await mediator.Send(
            new POS.Application.UseCases.Products.Queries.GetPosCatalog.GetPosCatalogQuery(
                request.CategoryId,
                request.Search,
                pageNumber,
                pageSize,
                request.StoreId),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<PosCatalogItemResponse>>.Ok(
            new PagedResponse<PosCatalogItemResponse>(
                paged.Items.Select(p => p.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [P01] Lấy danh sách sản phẩm phân trang với bộ lọc tên/danh mục/trạng thái.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductSummaryResponse>>>> GetProducts(
        [FromQuery] ProductFilterRequest request,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize   = Math.Min(100, Math.Max(1, request.PageSize));

        var result = await mediator.Send(
            new GetProductsQuery(
                request.Search,
                request.CategoryId,
                request.Status,
                pageNumber,
                pageSize,
                request.StoreId),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<ProductSummaryResponse>>.Ok(
            new PagedResponse<ProductSummaryResponse>(
                paged.Items.Select(p => p.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [P00-KPI] Lấy số liệu thống kê KPI chuẩn (Tổng sản phẩm, Đang kinh doanh, Tổng SKU, Tổng Danh mục).
    /// </summary>
    [HttpGet("kpi-summary")]
    public async Task<ActionResult<ApiResponse<ProductKpiSummaryResponse>>> GetProductKpiSummary(
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new POS.Application.UseCases.Products.Queries.GetProductKpiSummary.GetProductKpiSummaryQuery(storeId),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var data = result.Value!;
        return Ok(ApiResponse<ProductKpiSummaryResponse>.Ok(
            new ProductKpiSummaryResponse(
                data.TotalProducts,
                data.ActiveProducts,
                data.TotalSkus,
                data.TotalCategories)));
    }

    /// <summary>
    /// [P02] Lấy chi tiết sản phẩm theo ID (kèm danh sách SKU).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponse>>> GetProductById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ProductDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var skus = request.Skus?.Select(s => new Application.UseCases.Products.Commands.CreateProduct.CreateSkuInfo(
            s.SkuCode,
            s.Barcode,
            s.CostPrice,
            s.SellPrice,
            s.TaxRate,
            s.Attributes
        )).ToList().AsReadOnly();

        var command = new Application.UseCases.Products.Commands.CreateProduct.CreateProductCommand(
            request.Name,
            request.CategoryId,
            request.BaseUnit,
            request.Description,
            request.Brand,
            request.ImageUrl,
            skus
        );

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<Guid>.Ok(result.Value));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.UseCases.Products.Commands.UpdateProduct.UpdateProductCommand(
            id,
            request.Name,
            request.CategoryId,
            request.BaseUnit,
            request.Description,
            request.Brand,
            request.ImageUrl,
            request.Status
        );

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<Guid>.Ok(result.Value));
    }

    /// <summary>
    /// [P05] Import danh sách sản phẩm hàng loạt từ file Excel.
    /// Endpoint: POST /api/v1/products/bulk-import  (multipart/form-data, field: file)
    /// </summary>
    /// <remarks>
    /// Yêu cầu:
    /// - Content-Type: multipart/form-data
    /// - Form field: file (IFormFile)
    /// - Đuôi file: .xlsx hoặc .xls
    /// - Kích thước tối đa: 10 MB
    /// </remarks>
    
    /// <summary>
    /// [P05] Xem trước dữ liệu file Excel nhập danh mục sản phẩm & tồn ban đầu trước khi chính thức import.
    /// </summary>
    [HttpPost("bulk-import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportPreviewResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductImportPreviewResponse>>> PreviewBulkImport(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ProductImportPreviewResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code = "BulkImport.EmptyFile",
                    Message = "File tải lên không có dữ liệu hoặc rỗng.",
                    Type = POS.Domain.Common.ErrorType.Validation
                }));
        }

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !ext.Equals(".xls", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<ProductImportPreviewResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code = "BulkImport.InvalidExtension",
                    Message = "Chỉ chấp nhận file Excel (.xlsx, .xls).",
                    Type = POS.Domain.Common.ErrorType.Validation
                }));
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        POS.Application.Abstractions.Excel.ExcelReadResult<POS.Application.UseCases.Products.Commands.BulkImportProducts.ProductImportRowDto> readResult;
        try
        {
            readResult = excelReader.Read<POS.Application.UseCases.Products.Commands.BulkImportProducts.ProductImportRowDto>(fileBytes);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProductImportPreviewResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code = "BulkImport.ParseError",
                    Message = $"Không thể đọc cấu trúc file Excel: {ex.Message}",
                    Type = POS.Domain.Common.ErrorType.Validation
                }));
        }

        var totalRows = readResult.Rows.Count;
        var validRows = readResult.Rows.Where(r => r.Errors.Count == 0).ToList();
        var errorRows = readResult.Rows.Where(r => r.Errors.Count > 0).ToList();

        var errors = new List<string>(readResult.GlobalErrors);
        foreach (var errRow in errorRows.Take(20))
        {
            foreach (var err in errRow.Errors)
                errors.Add($"Dòng {errRow.RowNumber}: {err}");
        }

        var previewItems = readResult.Rows.Take(10).Select(r => new ProductImportPreviewRowDto(
            RowNumber: r.RowNumber,
            Name: r.Item.Name,
            CategoryName: r.Item.CategoryName,
            BaseUnit: r.Item.BaseUnit,
            SkuCode: r.Item.SkuCode,
            Barcode: r.Item.Barcode,
            CostPrice: r.Item.CostPrice,
            SellPrice: r.Item.SellPrice,
            TaxRate: r.Item.TaxRate,
            InitialStock: r.Item.InitialStock,
            MinStock: r.Item.MinStock,
            BatchNo: r.Item.BatchNo,
            ExpiryDate: r.Item.ExpiryDate,
            IsValid: r.Errors.Count == 0,
            ErrorMessage: r.Errors.Count > 0 ? string.Join("; ", r.Errors) : null
        )).ToList().AsReadOnly();

        var response = new ProductImportPreviewResponse(
            FileName: file.FileName,
            FileSizeBytes: file.Length,
            TotalRows: totalRows,
            ValidRowsCount: validRows.Count,
            ErrorRowsCount: errorRows.Count,
            PreviewItems: previewItems,
            Errors: errors.AsReadOnly()
        );

        return Ok(ApiResponse<ProductImportPreviewResponse>.Ok(response));
    }


    [HttpPost("bulk-import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB hard-limit ở tầng HTTP server
    [ProducesResponseType(typeof(ApiResponse<BulkImportResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkImportResultResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<BulkImportResultResponse>>> BulkImport(
        IFormFile file,
        [FromForm] decimal? defaultInitialStock = null,
        [FromForm] decimal? defaultMinStock = null,
        [FromForm] bool autoCreateCategories = true,
        CancellationToken cancellationToken = default)
    {
        // ── [FIX-2] Validation file upload tại Controller ──────────────────────

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<BulkImportResultResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code    = "BulkImport.FileEmpty",
                    Message = "File không được để trống.",
                    Type    = POS.Domain.Common.ErrorType.Validation
                }));
        }

        // --- Kiểm tra extension (.xlsx / .xls) ---
        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !ext.Equals(".xls",  StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<BulkImportResultResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code    = "BulkImport.InvalidExtension",
                    Message = "Chỉ chấp nhận file Excel (.xlsx, .xls).",
                    Type    = POS.Domain.Common.ErrorType.Validation
                }));
        }

        // --- Kiểm tra MIME type ---
        // [BUG-6] Bổ sung thêm "application/x-xls" và các MIME phổ biến từ WPS/LibreOffice
        var allowedMimeTypes = new[]
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "application/x-xls",
            "application/x-excel",
            "application/octet-stream" // một số client gửi generic MIME
        };
        if (!allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<BulkImportResultResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code    = "BulkImport.InvalidMimeType",
                    Message = $"MIME type không hợp lệ ({file.ContentType}). Chỉ chấp nhận file Excel.",
                    Type    = POS.Domain.Common.ErrorType.Validation
                }));
        }

        // --- Kiểm tra kích thước tối đa 10 MB ---
        const long maxSizeBytes = 10L * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            return BadRequest(ApiResponse<BulkImportResultResponse>.Fail(
                new POS.Contracts.V1.Common.ApiError
                {
                    Code    = "BulkImport.FileTooLarge",
                    Message = $"Kích thước file vượt quá giới hạn 10 MB (hiện tại: {file.Length / 1024 / 1024} MB).",
                    Type    = POS.Domain.Common.ErrorType.Validation
                }));
        }

        // ── Đọc file thành mảng byte ────────────────────────────────────────────
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        var command = new Application.UseCases.Products.Commands.BulkImportProducts.BulkImportProductsCommand(
            FileContent: fileBytes,
            FileName:    file.FileName,
            StoreId:     null,
            DefaultInitialStock: defaultInitialStock,
            DefaultMinStock:     defaultMinStock,
            AutoCreateCategories: autoCreateCategories);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var responseDto = result.Value!;
        var response = new BulkImportResultResponse(
            responseDto.TotalRows,
            responseDto.SuccessCount,
            responseDto.FailureCount,
            responseDto.Errors
        );

        return Ok(ApiResponse<BulkImportResultResponse>.Ok(response));
    }
}
