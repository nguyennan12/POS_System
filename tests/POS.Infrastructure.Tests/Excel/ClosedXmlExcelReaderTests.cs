using ClosedXML.Excel;
using FluentAssertions;
using POS.Application.Abstractions.Excel;
using POS.Infrastructure.Excel;

namespace POS.Infrastructure.Tests.Excel;

public class ClosedXmlExcelReaderTests
{
    private class TestItemDto
    {
        [ExcelColumn("SkuCode", Order = 1)]
        public string? SkuCode { get; set; }

        [ExcelColumn("Qty", Order = 2)]
        public decimal? Qty { get; set; }

        [ExcelColumn("ExpiryDate", Order = 3)]
        public DateOnly? ExpiryDate { get; set; }
    }

    [Fact]
    public void Read_ShouldParseDateOnlyCorrectly_FromDifferentFormats()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "SkuCode";
        ws.Cell(1, 2).Value = "Qty";
        ws.Cell(1, 3).Value = "ExpiryDate";

        // Row 2: Standard Date format
        ws.Cell(2, 1).Value = "SKU-001";
        ws.Cell(2, 2).Value = 10;
        ws.Cell(2, 3).Value = new DateTime(2026, 12, 31);

        // Row 3: Text dd/MM/yyyy
        ws.Cell(3, 1).Value = "SKU-002";
        ws.Cell(3, 2).Value = 25;
        ws.Cell(3, 3).Value = "15/08/2027";

        // Row 4: Text yyyy-MM-dd
        ws.Cell(4, 1).Value = "SKU-003";
        ws.Cell(4, 2).Value = 50;
        ws.Cell(4, 3).Value = "2028-05-20";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var reader = new ClosedXmlExcelReader();

        // Act
        var result = reader.Read<TestItemDto>(bytes);

        // Assert
        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(3);
        result.Rows.All(r => r.Errors.Count == 0).Should().BeTrue();

        result.Rows[0].Item.SkuCode.Should().Be("SKU-001");
        result.Rows[0].Item.Qty.Should().Be(10);
        result.Rows[0].Item.ExpiryDate.Should().Be(new DateOnly(2026, 12, 31));

        result.Rows[1].Item.SkuCode.Should().Be("SKU-002");
        result.Rows[1].Item.Qty.Should().Be(25);
        result.Rows[1].Item.ExpiryDate.Should().Be(new DateOnly(2027, 8, 15));

        result.Rows[2].Item.SkuCode.Should().Be("SKU-003");
        result.Rows[2].Item.Qty.Should().Be(50);
        result.Rows[2].Item.ExpiryDate.Should().Be(new DateOnly(2028, 5, 20));
    }
}
