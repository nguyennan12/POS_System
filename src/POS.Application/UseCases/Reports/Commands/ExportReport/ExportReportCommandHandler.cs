using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Excel;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Reports;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Commands.ExportReport;

public class ExportReportCommandHandler : ICommandHandler<ExportReportCommand, ExportReportJobResponse>
{
    private readonly IReportRepository _reportRepository;
    private readonly IExcelExporter _excelExporter;
    private readonly IReportExportJobStore _jobStore;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public ExportReportCommandHandler(
        IReportRepository reportRepository,
        IExcelExporter excelExporter,
        IReportExportJobStore jobStore,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _excelExporter = excelExporter;
        _jobStore = jobStore;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<ExportReportJobResponse>> Handle(
        ExportReportCommand command,
        CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        _jobStore.CreateJob(jobId);

        try
        {
            var p = command.Params ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            DateTimeOffset? from = null;
            if (p.TryGetValue("from", out var fromStr) && DateTimeOffset.TryParse(fromStr, out var parsedFrom))
            {
                from = parsedFrom;
            }

            DateTimeOffset? to = null;
            if (p.TryGetValue("to", out var toStr) && DateTimeOffset.TryParse(toStr, out var parsedTo))
            {
                to = parsedTo;
            }

            Guid? storeId = null;
            if (p.TryGetValue("storeId", out var storeIdStr) && Guid.TryParse(storeIdStr, out var parsedStoreId))
            {
                storeId = parsedStoreId;
            }

            Guid? employeeId = null;
            if (p.TryGetValue("employeeId", out var empIdStr) && Guid.TryParse(empIdStr, out var parsedEmpId))
            {
                employeeId = parsedEmpId;
            }

            Guid? categoryId = null;
            if (p.TryGetValue("categoryId", out var catIdStr) && Guid.TryParse(catIdStr, out var parsedCatId))
            {
                categoryId = parsedCatId;
            }

            int days = 30;
            if (p.TryGetValue("days", out var daysStr) && int.TryParse(daysStr, out var parsedDays))
            {
                days = parsedDays;
            }

            int limit = 10;
            if (p.TryGetValue("limit", out var limitStr) && int.TryParse(limitStr, out var parsedLimit))
            {
                limit = parsedLimit;
            }

            // Enforce store scope for non-chain-owners
            if (_currentUser.EmployeeId.HasValue)
            {
                var currentEmployee = await _employeeRepository.GetByIdAsync(_currentUser.EmployeeId.Value, cancellationToken);
                if (currentEmployee != null && !currentEmployee.IsChainOwner)
                {
                    storeId = currentEmployee.StoreId;
                }
            }

            byte[] fileBytes;
            string fileName;
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            var reportType = command.Type.ToLowerInvariant();

            switch (reportType)
            {
                case "revenue":
                {
                    var report = await _reportRepository.GetRevenueReportAsync(from, to, storeId, employeeId, cancellationToken);
                    fileBytes = _excelExporter.Export(report.Periods, options =>
                    {
                        options.SheetName = "Báo cáo doanh thu";
                        options.Title = "BÁO CÁO DOANH THU BÁN HÀNG";
                        options.FontName = "Segoe UI";
                        options.ShowTotalRow = true;
                        options.TotalRowLabel = "Tổng cộng";
                        options.TotalRowLabelColumnIndex = 0;
                        options.AddColumn("Kỳ / Ngày", x => x.Period, width: 16);
                        options.AddColumn("Doanh thu", x => x.Revenue, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Giảm giá", x => x.Discount, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Tiền thuế", x => x.Tax, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Doanh thu thuần", x => x.NetRevenue, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Số đơn hàng", x => x.OrderCount, numberFormat: "#,##0", isTotalColumn: true);
                    });
                    fileName = $"BaoCao_DoanhThu_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;
                }

                case "inventory":
                {
                    var report = await _reportRepository.GetInventoryReportAsync(storeId, categoryId, cancellationToken);
                    fileBytes = _excelExporter.Export(report.Items, options =>
                    {
                        options.SheetName = "Báo cáo tồn kho";
                        options.Title = "BÁO CÁO TỒN KHO HIỆN TẠI";
                        options.FontName = "Segoe UI";
                        options.ShowTotalRow = true;
                        options.TotalRowLabel = "Tổng cộng";
                        options.TotalRowLabelColumnIndex = 0;
                        options.AddColumn("Mã SKU", x => x.SkuCode, width: 18);
                        options.AddColumn("Tên sản phẩm", x => x.ProductName, width: 32);
                        options.AddColumn("Danh mục", x => x.CategoryName, width: 22);
                        options.AddColumn("Tồn kho", x => x.QtyOnHand, numberFormat: "#,##0.##", isTotalColumn: true);
                        options.AddColumn("Giá vốn", x => x.CostPrice, numberFormat: "#,##0");
                        options.AddColumn("Giá trị tồn", x => x.StockValue, numberFormat: "#,##0", isTotalColumn: true);
                    });
                    fileName = $"BaoCao_TonKho_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;
                }

                case "top-selling":
                {
                    var items = await _reportRepository.GetTopSellingAsync(from, to, limit, storeId, cancellationToken);
                    fileBytes = _excelExporter.Export(items, options =>
                    {
                        options.SheetName = "Top bán chạy";
                        options.Title = "BÁO CÁO SẢN PHẨM BÁN CHẠY";
                        options.FontName = "Segoe UI";
                        options.ShowTotalRow = true;
                        options.TotalRowLabel = "Tổng cộng";
                        options.TotalRowLabelColumnIndex = 0;
                        options.AddColumn("Mã SKU", x => x.SkuCode, width: 18);
                        options.AddColumn("Tên sản phẩm", x => x.ProductName, width: 32);
                        options.AddColumn("Số lượng bán", x => x.QuantitySold, numberFormat: "#,##0.##", isTotalColumn: true);
                        options.AddColumn("Doanh thu", x => x.TotalRevenue, numberFormat: "#,##0", isTotalColumn: true);
                    });
                    fileName = $"BaoCao_TopBanChay_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;
                }

                case "slow-moving":
                {
                    var items = await _reportRepository.GetSlowMovingItemsAsync(days, storeId, cancellationToken);
                    fileBytes = _excelExporter.Export(items, options =>
                    {
                        options.SheetName = "Hàng chậm bán";
                        options.Title = $"BÁO CÁO HÀNG CHẬM LUÂN CHUYỂN (>{days} NGÀY)";
                        options.FontName = "Segoe UI";
                        options.ShowTotalRow = true;
                        options.TotalRowLabel = "Tổng cộng";
                        options.TotalRowLabelColumnIndex = 0;
                        options.AddColumn("Mã SKU", x => x.SkuCode, width: 18);
                        options.AddColumn("Tên sản phẩm", x => x.ProductName, width: 32);
                        options.AddColumn("Số lượng tồn", x => x.QtyOnHand, numberFormat: "#,##0.##", isTotalColumn: true);
                        options.AddColumn("Số ngày không bán", x => x.DaysWithoutSale, numberFormat: "#,##0");
                        options.AddColumn("Ngày bán cuối", x => x.LastSoldAt.HasValue ? x.LastSoldAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa từng bán", width: 22);
                    });
                    fileName = $"BaoCao_HangChamBan_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;
                }

                case "shift":
                {
                    if (!p.TryGetValue("shiftId", out var shiftIdStr) || !Guid.TryParse(shiftIdStr, out var shiftId))
                    {
                        _jobStore.FailJob(jobId, "Cần cung cấp tham số shiftId để xuất báo cáo ca.");
                        return new Error(ErrorType.Validation, "Export.MissingShiftId", "Cần cung cấp tham số shiftId để xuất báo cáo ca.");
                    }

                    var report = await _reportRepository.GetShiftReportAsync(shiftId, cancellationToken);
                    if (report is null)
                    {
                        _jobStore.FailJob(jobId, "Không tìm thấy thông tin ca làm việc.");
                        return new Error(ErrorType.NotFound, "Shift.NotFound", "Không tìm thấy thông tin ca làm việc.");
                    }

                    fileBytes = _excelExporter.Export(report.Orders, options =>
                    {
                        options.SheetName = "Báo cáo ca";
                        options.Title = $"BÁO CÁO CA LÀM VIỆC - {report.Shift.RegisterName} ({report.Shift.EmployeeName})";
                        options.FontName = "Segoe UI";
                        options.ShowTotalRow = true;
                        options.TotalRowLabel = "Tổng cộng";
                        options.TotalRowLabelColumnIndex = 0;
                        options.AddColumn("Mã đơn", x => x.Id.ToString()[..8].ToUpperInvariant(), width: 14);
                        options.AddColumn("Khách hàng", x => x.CustomerName ?? "Khách vãng lai", width: 24);
                        options.AddColumn("Số điện thoại", x => x.CustomerPhone ?? "-", width: 16);
                        options.AddColumn("Trạng thái", x => x.Status, width: 14);
                        options.AddColumn("Tổng tiền", x => x.GrandTotal, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Số món", x => x.TotalItems, numberFormat: "#,##0", isTotalColumn: true);
                        options.AddColumn("Thời gian", x => x.CreatedAt.ToString("dd/MM/yyyy HH:mm"), width: 20);
                    });
                    fileName = $"BaoCao_Ca_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;
                }

                default:
                    _jobStore.FailJob(jobId, $"Loại báo cáo '{command.Type}' không được hỗ trợ.");
                    return new Error(ErrorType.Validation, "Export.UnsupportedType", $"Loại báo cáo '{command.Type}' không được hỗ trợ.");
            }

            _jobStore.CompleteJob(jobId, fileName, contentType, fileBytes);

            return Result<ExportReportJobResponse>.Success(
                new ExportReportJobResponse(jobId, "Completed", DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            _jobStore.FailJob(jobId, ex.Message);
            throw;
        }
    }
}
