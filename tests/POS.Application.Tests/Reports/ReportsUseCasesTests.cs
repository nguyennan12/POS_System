using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Excel;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Reports.Commands.ExportReport;
using POS.Application.UseCases.Reports.Queries.GetDashboardSummary;
using POS.Application.UseCases.Reports.Queries.GetExportJobStatus;
using POS.Application.UseCases.Reports.Queries.GetExportReportFile;
using POS.Application.UseCases.Reports.Queries.GetInventoryReport;
using POS.Application.UseCases.Reports.Queries.GetRevenueReport;
using POS.Application.UseCases.Reports.Queries.GetShiftReport;
using POS.Application.UseCases.Reports.Queries.GetSlowMovingReport;
using POS.Application.UseCases.Reports.Queries.GetTopSellingReport;
using POS.Contracts.V1.Orders;
using POS.Contracts.V1.Reports;
using POS.Contracts.V1.Shifts;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Stores;

namespace POS.Application.Tests.Reports;

public class ReportsUseCasesTests
{
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IExcelExporter _excelExporter = Substitute.For<IExcelExporter>();
    private readonly IReportExportJobStore _jobStore = Substitute.For<IReportExportJobStore>();

    [Fact]
    public async Task GetRevenueReport_WhenUserIsChainOwner_ShouldUseRequestedStoreId()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var requestedStoreId = Guid.NewGuid();
        _currentUser.EmployeeId.Returns(ownerId);

        var chainOwner = new Employee(
            name: "Owner",
            username: "0901234567",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: true,
            id: ownerId);
        _employeeRepository.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(chainOwner);

        var expectedResponse = new RevenueReportResponse(
            TotalRevenue: 10_000_000m,
            TotalDiscount: 500_000m,
            TotalTax: 950_000m,
            NetRevenue: 10_450_000m,
            TotalOrders: 20,
            Periods: new List<RevenueByPeriodItem>
            {
                new("2026-10-14", 10_000_000m, 500_000m, 950_000m, 10_450_000m, 20)
            });

        _reportRepository.GetRevenueReportAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            requestedStoreId,
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(expectedResponse);

        var handler = new GetRevenueReportQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(
            new GetRevenueReportQuery(StoreId: requestedStoreId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        await _reportRepository.Received(1).GetRevenueReportAsync(
            null, null, requestedStoreId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRevenueReport_WhenUserIsNotChainOwner_ShouldEnforceUserStoreId()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var userStoreId = Guid.NewGuid();
        var foreignStoreId = Guid.NewGuid();
        _currentUser.EmployeeId.Returns(employeeId);

        var staff = new Employee(
            name: "Staff",
            username: "0909999999",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: userStoreId,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(staff);

        var expectedResponse = new RevenueReportResponse(0, 0, 0, 0, 0, Array.Empty<RevenueByPeriodItem>());
        _reportRepository.GetRevenueReportAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            userStoreId,
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(expectedResponse);

        var handler = new GetRevenueReportQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(
            new GetRevenueReportQuery(StoreId: foreignStoreId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should enforce userStoreId instead of foreignStoreId
        await _reportRepository.Received(1).GetRevenueReportAsync(
            null, null, userStoreId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopSellingReport_ShouldReturnItemsFromRepository()
    {
        // Arrange
        var items = new List<TopSellingProductItem>
        {
            new(Guid.NewGuid(), "SKU01", "CÃ  phÃª Ä‘en", 50, 1_500_000m),
            new(Guid.NewGuid(), "SKU02", "Báº¡c xá»‰u", 40, 1_400_000m)
        };

        _reportRepository.GetTopSellingAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            5,
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(items);

        var handler = new GetTopSellingReportQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(new GetTopSellingReportQuery(Limit: 5), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ProductName.Should().Be("CÃ  phÃª Ä‘en");
    }

    [Fact]
    public async Task GetInventoryReport_ShouldReturnMetrics()
    {
        // Arrange
        var expected = new InventoryReportResponse(
            TotalSkus: 10,
            TotalStockValue: 50_000_000m,
            OutOfStockCount: 2,
            LowStockCount: 3,
            Items: Array.Empty<InventoryReportItem>());

        _reportRepository.GetInventoryReportAsync(
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GetInventoryReportQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(new GetInventoryReportQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalStockValue.Should().Be(50_000_000m);
        result.Value.OutOfStockCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSlowMovingReport_ShouldReturnSlowMovingItems()
    {
        // Arrange
        var items = new List<SlowMovingItemResponse>
        {
            new(Guid.NewGuid(), "SKU_SLOW", "TrÃ  lÃ i tÃºi lá»c", 100, 45, DateTimeOffset.UtcNow.AddDays(-45))
        };

        _reportRepository.GetSlowMovingItemsAsync(
            30,
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(items);

        var handler = new GetSlowMovingReportQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(new GetSlowMovingReportQuery(Days: 30), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].DaysWithoutSale.Should().Be(45);
    }

    [Fact]
    public async Task GetShiftReport_WhenShiftExists_ShouldReturnShiftAndOrders()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var shiftSummary = new ShiftSummaryResponse(
            ShiftId: shiftId,
            RegisterId: Guid.NewGuid(),
            RegisterName: "Quáº§y 1",
            EmployeeId: Guid.NewGuid(),
            EmployeeName: "Thu ngÃ¢n A",
            OpeningCash: 1_000_000m,
            TotalCashSales: 500_000m,
            TotalCardSales: 300_000m,
            TotalQrSales: 200_000m,
            TotalRefunds: 0m,
            ExpectedCash: 1_500_000m,
            ActualCash: 1_500_000m,
            Difference: 0m,
            OrderCount: 5,
            OpenedAt: DateTimeOffset.UtcNow.AddHours(-8),
            ClosedAt: DateTimeOffset.UtcNow);

        var expected = new ShiftReportResponse(shiftSummary, Array.Empty<OrderSummaryResponse>());
        _reportRepository.GetShiftReportAsync(shiftId, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GetShiftReportQueryHandler(_reportRepository);

        // Act
        var result = await handler.Handle(new GetShiftReportQuery(shiftId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Shift.RegisterName.Should().Be("Quáº§y 1");
    }

    [Fact]
    public async Task GetShiftReport_WhenShiftNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        _reportRepository.GetShiftReportAsync(shiftId, Arg.Any<CancellationToken>()).Returns((ShiftReportResponse?)null);

        var handler = new GetShiftReportQueryHandler(_reportRepository);

        // Act
        var result = await handler.Handle(new GetShiftReportQuery(shiftId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ExportReport_Revenue_ShouldGenerateExcelAndSaveToJobStore()
    {
        // Arrange
        var fakeBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // zip/xlsx header
        _excelExporter.Export(Arg.Any<IEnumerable<RevenueByPeriodItem>>(), Arg.Any<Action<ExcelExportOptions<RevenueByPeriodItem>>>())
            .Returns(fakeBytes);

        var revenueReport = new RevenueReportResponse(
            1_000_000m, 50_000m, 95_000m, 1_045_000m, 2,
            new List<RevenueByPeriodItem> { new("2026-10-14", 1_000_000m, 50_000m, 95_000m, 1_045_000m, 2) });

        _reportRepository.GetRevenueReportAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(revenueReport);

        var handler = new ExportReportCommandHandler(
            _reportRepository,
            _excelExporter,
            _jobStore,
            _currentUser,
            _employeeRepository);

        var command = new ExportReportCommand("revenue", "xlsx");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");

        _jobStore.Received(1).CreateJob(result.Value.JobId);
        _jobStore.Received(1).CompleteJob(
            result.Value.JobId,
            Arg.Is<string>(fn => fn.StartsWith("BaoCao_DoanhThu_")),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fakeBytes);
    }

    [Fact]
    public async Task ExportReport_Inventory_ShouldGenerateExcelAndSaveToJobStore()
    {
        // Arrange
        var fakeBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _excelExporter.Export(Arg.Any<IEnumerable<InventoryReportItem>>(), Arg.Any<Action<ExcelExportOptions<InventoryReportItem>>>())
            .Returns(fakeBytes);

        var inventoryReport = new InventoryReportResponse(
            1, 100_000m, 0, 0,
            new List<InventoryReportItem> { new(Guid.NewGuid(), "SKU1", "SP 1", "DM 1", 10, 10_000m, 100_000m) });

        _reportRepository.GetInventoryReportAsync(
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>()).Returns(inventoryReport);

        var handler = new ExportReportCommandHandler(
            _reportRepository,
            _excelExporter,
            _jobStore,
            _currentUser,
            _employeeRepository);

        var command = new ExportReportCommand("inventory", "xlsx");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");

        _jobStore.Received(1).CompleteJob(
            result.Value.JobId,
            Arg.Is<string>(fn => fn.StartsWith("BaoCao_TonKho_")),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fakeBytes);
    }

    [Fact]
    public async Task GetExportJobStatus_WhenJobExists_ShouldReturnStatusResponse()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new ReportExportJob(
            JobId: jobId,
            Status: "Completed",
            CreatedAt: DateTimeOffset.UtcNow,
            FileName: "test.xlsx",
            FileBytes: new byte[] { 1, 2, 3 });

        _jobStore.GetJob(jobId).Returns(job);

        var handler = new GetExportJobStatusQueryHandler(_jobStore);

        // Act
        var result = await handler.Handle(new GetExportJobStatusQuery(jobId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");
        result.Value.DownloadUrl.Should().Be($"/api/v1/reports/export/{jobId}/download");
    }

    [Fact]
    public async Task GetExportReportFile_WhenFileReady_ShouldReturnFileDto()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new ReportExportJob(
            JobId: jobId,
            Status: "Completed",
            CreatedAt: DateTimeOffset.UtcNow,
            FileName: "report.xlsx",
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileBytes: new byte[] { 1, 2, 3 });

        _jobStore.GetJob(jobId).Returns(job);

        var handler = new GetExportReportFileQueryHandler(_jobStore);

        // Act
        var result = await handler.Handle(new GetExportReportFileQuery(jobId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("report.xlsx");
        result.Value.FileBytes.Should().Equal(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public void ExportReportCommandValidator_ShouldValidateTypeAndFormat()
    {
        // Arrange
        var validator = new ExportReportCommandValidator();

        // Valid
        var validCommand = new ExportReportCommand("revenue", "xlsx");
        validator.Validate(validCommand).IsValid.Should().BeTrue();

        // Invalid type
        var invalidTypeCommand = new ExportReportCommand("invalid_type", "xlsx");
        validator.Validate(invalidTypeCommand).IsValid.Should().BeFalse();

        // Invalid format
        var invalidFormatCommand = new ExportReportCommand("revenue", "mp4");
        validator.Validate(invalidFormatCommand).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardSummary_WhenUserIsChainOwner_ShouldAllowNullOrSpecifiedStoreId()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _currentUser.EmployeeId.Returns(ownerId);

        var chainOwner = new Employee(
            name: "Owner",
            username: "0901234567",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: true,
            id: ownerId);
        _employeeRepository.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(chainOwner);

        var expectedResponse = new DashboardSummaryResponse(
            TodayRevenue: 10_000_000m,
            TodayOrders: 30,
            AverageOrderValue: 333_333.33m,
            RevenueGrowthPercentage: 15.0m,
            RevenueChart: new List<DailyRevenueChartItem>
            {
                new(DateOnly.FromDateTime(DateTime.UtcNow), 10_000_000m, 30)
            },
            TopSellingProducts: new List<TopSellingProductItem>(),
            Alerts: new DashboardAlerts(1, 0, 0)
        );

        _reportRepository.GetDashboardSummaryAsync(null, 7, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var handler = new GetDashboardSummaryQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act
        var result = await handler.Handle(new GetDashboardSummaryQuery(StoreId: null, Days: 7), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        await _reportRepository.Received(1).GetDashboardSummaryAsync(null, 7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardSummary_WhenUserIsNotChainOwner_ShouldEnforceAssignedStoreId()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var assignedStoreId = Guid.NewGuid();
        var requestedStoreId = Guid.NewGuid();
        _currentUser.EmployeeId.Returns(employeeId);

        var manager = new Employee(
            name: "Manager",
            username: "0908888888",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: assignedStoreId,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(manager);

        var expectedResponse = new DashboardSummaryResponse(
            TodayRevenue: 5_000_000m,
            TodayOrders: 15,
            AverageOrderValue: 333_333.33m,
            RevenueGrowthPercentage: -5.0m,
            RevenueChart: new List<DailyRevenueChartItem>(),
            TopSellingProducts: new List<TopSellingProductItem>(),
            Alerts: new DashboardAlerts(0, 0, 0)
        );

        _reportRepository.GetDashboardSummaryAsync(assignedStoreId, 14, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var handler = new GetDashboardSummaryQueryHandler(_reportRepository, _currentUser, _employeeRepository);

        // Act - Manager requests a different storeId, but handler must enforce assignedStoreId
        var result = await handler.Handle(new GetDashboardSummaryQuery(StoreId: requestedStoreId, Days: 14), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        await _reportRepository.Received(1).GetDashboardSummaryAsync(assignedStoreId, 14, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetDashboardSummaryQueryValidator_ShouldValidateDaysRange()
    {
        // Arrange
        var validator = new GetDashboardSummaryQueryValidator();

        // Valid
        validator.Validate(new GetDashboardSummaryQuery(null, 7)).IsValid.Should().BeTrue();
        validator.Validate(new GetDashboardSummaryQuery(null, 30)).IsValid.Should().BeTrue();

        // Invalid (less than 1 or greater than 90)
        validator.Validate(new GetDashboardSummaryQuery(null, 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetDashboardSummaryQuery(null, 91)).IsValid.Should().BeFalse();
    }
}
