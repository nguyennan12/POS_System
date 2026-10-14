using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.UseCases.Reports.Commands.ExportReport;
using POS.Application.UseCases.Reports.Queries.GetDashboardSummary;
using POS.Application.UseCases.Reports.Queries.GetExportJobStatus;
using POS.Application.UseCases.Reports.Queries.GetExportReportFile;
using POS.Application.UseCases.Reports.Queries.GetInventoryReport;
using POS.Application.UseCases.Reports.Queries.GetRevenueReport;
using POS.Application.UseCases.Reports.Queries.GetShiftReport;
using POS.Application.UseCases.Reports.Queries.GetSlowMovingReport;
using POS.Application.UseCases.Reports.Queries.GetTopSellingReport;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Reports;
using POS.Contracts.V1.Shifts;
using POS.Domain.Common;

namespace POS.Api.Tests.Reports;

public class ReportsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    [Fact]
    public async Task GetRevenueReport_ShouldDispatchQuery_AndReturnOk()
    {
        // Arrange
        var controller = new ReportsController(_sender);
        var expected = new RevenueReportResponse(100, 10, 8, 98, 1, Array.Empty<RevenueByPeriodItem>());
        _sender.Send(Arg.Any<GetRevenueReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<RevenueReportResponse>.Success(expected));

        // Act
        var result = await controller.GetRevenueReport(new RevenueReportFilterRequest(), CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<RevenueReportResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().Be(expected);
    }

    [Fact]
    public async Task GetTopSellingReport_ShouldDispatchQuery_AndReturnOk()
    {
        // Arrange
        var controller = new ReportsController(_sender);
        var expected = new List<TopSellingProductItem>
        {
            new(Guid.NewGuid(), "SKU1", "SP 1", 10, 500_000m)
        };
        _sender.Send(Arg.Any<GetTopSellingReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TopSellingProductItem>>.Success(expected));

        // Act
        var result = await controller.GetTopSellingReport(new TopSellingReportFilterRequest(), CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<TopSellingProductItem>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetInventoryReport_ShouldDispatchQuery_AndReturnOk()
    {
        // Arrange
        var controller = new ReportsController(_sender);
        var expected = new InventoryReportResponse(5, 5_000_000m, 1, 1, Array.Empty<InventoryReportItem>());
        _sender.Send(Arg.Any<GetInventoryReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<InventoryReportResponse>.Success(expected));

        // Act
        var result = await controller.GetInventoryReport(new InventoryReportFilterRequest(), CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<InventoryReportResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().Be(expected);
    }

    [Fact]
    public async Task GetSlowMovingReport_ShouldDispatchQuery_AndReturnOk()
    {
        // Arrange
        var controller = new ReportsController(_sender);
        var expected = new List<SlowMovingItemResponse>
        {
            new(Guid.NewGuid(), "SKU_SLOW", "Món chậm bán", 50, 40, null)
        };
        _sender.Send(Arg.Any<GetSlowMovingReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<SlowMovingItemResponse>>.Success(expected));

        // Act
        var result = await controller.GetSlowMovingReport(days: 30, cancellationToken: CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<SlowMovingItemResponse>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetShiftReport_ShouldDispatchQuery_AndReturnOk()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var controller = new ReportsController(_sender);
        var shiftSummary = new ShiftSummaryResponse(
            shiftId, Guid.NewGuid(), "Quầy 1", Guid.NewGuid(), "NV", 100, 100, 0, 0, 0, 200, 200, 0, 1, DateTimeOffset.UtcNow, null);
        var expected = new ShiftReportResponse(shiftSummary, Array.Empty<POS.Contracts.V1.Orders.OrderSummaryResponse>());

        _sender.Send(Arg.Any<GetShiftReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ShiftReportResponse>.Success(expected));

        // Act
        var result = await controller.GetShiftReport(shiftId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ShiftReportResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().Be(expected);
    }

    [Fact]
    public async Task ExportReport_ShouldDispatchCommand_AndReturnJobResponse()
    {
        // Arrange
        var controller = new ReportsController(_sender);
        var jobId = Guid.NewGuid();
        var expected = new ExportReportJobResponse(jobId, "Completed", DateTimeOffset.UtcNow);

        _sender.Send(Arg.Any<ExportReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExportReportJobResponse>.Success(expected));

        // Act
        var result = await controller.ExportReport(new ExportReportRequest("revenue", "xlsx"), CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ExportReportJobResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.JobId.Should().Be(jobId);
    }

    [Fact]
    public async Task DownloadExportReport_WhenCompleted_ShouldReturnFileContentResult()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var controller = new ReportsController(_sender);
        var fileDto = new ExportReportFileDto("report.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1, 2, 3]);

        _sender.Send(Arg.Any<GetExportReportFileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExportReportFileDto>.Success(fileDto));

        // Act
        var result = await controller.DownloadExportReport(jobId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileDownloadName.Should().Be("report.xlsx");
        fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        fileResult.FileContents.Should().Equal([1, 2, 3]);
    }
}
