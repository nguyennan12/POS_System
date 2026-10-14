using ClosedXML.Excel;
using FluentAssertions;
using POS.Application.Abstractions.Excel;
using POS.Infrastructure.Excel;

namespace POS.Infrastructure.Tests.Excel;

public class ClosedXmlExcelExporterTests
{
    private record SampleReportItem(string Name, decimal Amount, int Quantity);

    [Fact]
    public void Export_ShouldApplySegoeUIFont_AndRenderTotalRowCorrectly()
    {
        // Arrange
        var exporter = new ClosedXmlExcelExporter();
        var items = new List<SampleReportItem>
        {
            new("Sản phẩm A", 100_000m, 2),
            new("Sản phẩm B", 250_000m, 5),
            new("Sản phẩm C", 150_000m, 3),
        };

        // Act
        var bytes = exporter.Export(items, options =>
        {
            options.SheetName = "Báo cáo mẫu";
            options.Title = "BÁO CÁO THỬ NGHIỆM";
            options.FontName = "Segoe UI";
            options.ShowTotalRow = true;
            options.TotalRowLabel = "Tổng cộng";
            options.TotalRowLabelColumnIndex = 0;
            options.AddColumn("Tên mặt hàng", x => x.Name, width: 25);
            options.AddColumn("Số lượng", x => x.Quantity, numberFormat: "#,##0", isTotalColumn: true);
            options.AddColumn("Doanh số", x => x.Amount, numberFormat: "#,##0", isTotalColumn: true);
        });

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Báo cáo mẫu");
        worksheet.Should().NotBeNull();

        // Check Font name
        worksheet.Style.Font.FontName.Should().Be("Segoe UI");

        // Title row is row 1
        worksheet.Cell(1, 1).GetString().Should().Be("BÁO CÁO THỬ NGHIỆM");

        // Header row is row 3 (after title + empty line)
        worksheet.Cell(3, 1).GetString().Should().Be("Tên mặt hàng");
        worksheet.Cell(3, 2).GetString().Should().Be("Số lượng");
        worksheet.Cell(3, 3).GetString().Should().Be("Doanh số");

        // Data rows: 4, 5, 6
        worksheet.Cell(4, 1).GetString().Should().Be("Sản phẩm A");
        worksheet.Cell(4, 2).GetDouble().Should().Be(2);
        worksheet.Cell(4, 3).GetDouble().Should().Be(100_000);

        // Total row should be row 7 (header + 3 items + 1)
        int totalRowIndex = 7;
        var labelCell = worksheet.Cell(totalRowIndex, 1);
        labelCell.GetString().Should().Be("Tổng cộng");
        labelCell.Style.Font.Bold.Should().BeTrue();

        var totalQtyCell = worksheet.Cell(totalRowIndex, 2);
        totalQtyCell.GetDouble().Should().Be(10); // 2 + 5 + 3 = 10
        totalQtyCell.Style.Font.Bold.Should().BeTrue();

        var totalAmountCell = worksheet.Cell(totalRowIndex, 3);
        totalAmountCell.GetDouble().Should().Be(500_000); // 100k + 250k + 150k = 500k
        totalAmountCell.Style.Font.Bold.Should().BeTrue();

        // Background of total row is styled
        worksheet.Cell(totalRowIndex, 1).Style.Fill.BackgroundColor.Should().NotBeNull();
        worksheet.Cell(totalRowIndex, 1).Style.Border.TopBorder.Should().Be(XLBorderStyleValues.Thin);
        worksheet.Cell(totalRowIndex, 1).Style.Border.BottomBorder.Should().Be(XLBorderStyleValues.Double);
    }

    [Fact]
    public void Export_WithCustomTotalSelector_ShouldRenderCustomTotal()
    {
        // Arrange
        var exporter = new ClosedXmlExcelExporter();
        var items = new List<SampleReportItem>
        {
            new("Item 1", 100m, 1),
            new("Item 2", 200m, 2),
        };

        // Act
        var bytes = exporter.Export(items, options =>
        {
            options.SheetName = "CustomTotal";
            options.ShowTotalRow = true;
            options.AddColumn("Tên", x => x.Name);
            options.AddColumn("Custom Col", x => x.Amount, customTotalSelector: list => 999m);
        });

        // Assert
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("CustomTotal");

        // Total row (row 3: row 1 header, row 2-3 data -> total is row 4)
        worksheet.Cell(4, 2).GetDouble().Should().Be(999);
    }
}
