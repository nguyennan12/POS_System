using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;
using POS.Infrastructure.Reports;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit.Abstractions;

namespace POS.Infrastructure.Tests.Invoices;

public class ReceiptRendererTests(ITestOutputHelper output)
{
    private static readonly IOptions<ReceiptOptions> Options = Microsoft.Extensions.Options.Options.Create(new ReceiptOptions());

    [Fact]
    public void Pdf_EmbedsVietnameseTextAndRendersTwentyItemsInUnderOneSecondAfterWarmup()
    {
        var renderer = new QuestPdfInvoiceRenderer(Options);
        var invoice = ReceiptSample.Create(20, reprint: true);
        renderer.Render(invoice);
        var timer = Stopwatch.StartNew();
        var bytes = renderer.Render(invoice);
        timer.Stop();
        output.WriteLine($"20-item warm PDF: {timer.Elapsed.TotalMilliseconds:F1} ms, {bytes.Length} bytes");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"Rendering took {timer.Elapsed}.");
        ExportPreview("invoice-20-items.pdf", bytes);
        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join("\n", pdf.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)))
            .Normalize(NormalizationForm.FormC);
        foreach (var expected in new[] { "HÓA ĐƠN BÁN HÀNG", "Nguyễn Huệ", "Nguyễn Thị Hồng",
            "Trần Văn Hải", "BẢN SAO / REPRINT", "HD-TEST-20261024-000001", "Cà phê", "Tiền thừa" })
            Assert.Contains(expected, text);
    }

    [Theory]
    [InlineData(58, 384)]
    [InlineData(80, 576)]
    public void EscPos_MatchesBinarySnapshotAndContainsValidRasterInitFeedAndCut(int width, int dots)
    {
        var bytes = new EscPosReceiptRenderer(Options).Render(ReceiptSample.Create(reprint: true), width);
        Assert.Equal(new byte[] { 0x1b, 0x40 }, bytes[..2]);
        Assert.Equal(new byte[] { 0x1b, 0x64, 3, 0x1d, 0x56, 0 }, bytes[^6..]);
        var raster = FindRaster(bytes);
        Assert.True(raster >= 0);
        var byteWidth = bytes[raster + 4] | bytes[raster + 5] << 8;
        var height = bytes[raster + 6] | bytes[raster + 7] << 8;
        Assert.Equal(dots / 8, byteWidth);
        Assert.True(height > 0);
        Assert.Equal(raster + 8 + byteWidth * height + 6, bytes.Length);
        var name = $"receipt-{width}.bin";
        ExportPreview(name, bytes);
        if (Environment.GetEnvironmentVariable("UPDATE_RECEIPT_SNAPSHOTS") == "1")
        {
            var directory = Path.Combine(SourceDirectory(), "Snapshots");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name), bytes);
        }
        else
        {
            var snapshot = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Invoices", "Snapshots", name));
            Assert.Equal(snapshot, bytes);
        }
    }

    [Theory]
    [InlineData(58, 32)]
    [InlineData(80, 48)]
    public void LongVietnameseNamesAndCombiningMarks_WrapWithoutLosingTextOrExceedingColumns(int width, int columns)
    {
        var name = string.Concat(Enumerable.Repeat("CàphêsữađáViệtNam", 10));
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var lines = ReceiptTextLayout.Wrap(decomposed, columns);
        Assert.Equal(name, string.Concat(lines));
        Assert.Equal(lines, ReceiptTextLayout.Wrap(name, columns));
        Assert.All(lines, line => Assert.InRange(ReceiptTextLayout.DisplayWidth(line), 0, columns));
        var invoice = ReceiptSample.Create();
        var receiptLines = ReceiptTextLayout.Build(invoice, width);
        Assert.All(receiptLines, line => Assert.InRange(ReceiptTextLayout.DisplayWidth(line.Text), 0, columns));
        var changed = invoice with { Receipt = invoice.Receipt with
        { Items = invoice.Receipt.Items.Select(i => i with { ProductName = i.ProductName.Normalize(NormalizationForm.FormD) }).ToArray() } };
        var renderer = new EscPosReceiptRenderer(Options);
        Assert.Equal(renderer.Render(invoice, width), renderer.Render(changed, width));
    }

    [Fact]
    public void TextMode_FallsBackToIdenticalRasterWhenCharacterCannotBeEncoded()
    {
        var invoice = ReceiptSample.Create();
        var asciiOnly = Microsoft.Extensions.Options.Options.Create(new ReceiptOptions { RenderMode = "Text", CodePage = 20127 });
        var expected = new EscPosReceiptRenderer(Options).Render(invoice, 58);
        Assert.Equal(expected, new EscPosReceiptRenderer(asciiOnly).Render(invoice, 58));
        // Windows-1258 must also fall back rather than substitute unsupported glyphs.
        var greek = invoice with { BuyerName = "Ω" };
        Assert.Equal(new EscPosReceiptRenderer(Options).Render(greek, 58),
            new EscPosReceiptRenderer(Microsoft.Extensions.Options.Options.Create(new ReceiptOptions { RenderMode = "Text" })).Render(greek, 58));
    }

    [Fact]
    public void Windows1258TextMode_RoundTripsVietnameseWithoutSubstitution()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ReceiptOptions { RenderMode = "Text" });
        var bytes = new EscPosReceiptRenderer(options).Render(ReceiptSample.Create(reprint: true), 58);
        Assert.Equal(-1, FindRaster(bytes));
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var text = ReceiptFormatting.Text(Encoding.GetEncoding(1258).GetString(bytes));
        Assert.Contains("BẢN SAO / REPRINT", text);
        Assert.Contains("Cà phê sữa đá", text);
        Assert.Contains("Nguyễn Thị Hồng", text);
        Assert.DoesNotContain("?", text);
    }

    [Fact]
    public void ExplicitVietnameseCombiningSequences_NormalizeEvenInInvariantRuntime()
    {
        Assert.Equal("Cà phê sữa đá", ReceiptFormatting.Text("Ca\u0300 phe\u0302 su\u031b\u0303a đa\u0301"));
        Assert.Equal("ặ ệ Ộ Ữ", ReceiptFormatting.Text("a\u0323\u0306 e\u0323\u0302 O\u0323\u0302 U\u031b\u0303"));
        Assert.Equal("Cà phê sữa đá", Assert.Single(ReceiptTextLayout.Wrap("Ca\u0300 phe\u0302 su\u031b\u0303a đa\u0301", 32)));
    }

    [Fact]
    public void Formatting_IsIndependentOfCurrentCultureAndPreservesFractionalAmounts()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal("1.234.567 ₫", ReceiptFormatting.Money(1234567));
            Assert.Equal("1.234,50 ₫", ReceiptFormatting.Money(1234.50m));
            Assert.Equal("0 ₫", ReceiptFormatting.Money(0));
            Assert.Equal("1,125", ReceiptFormatting.Quantity(1.125m));
            Assert.Equal("24/10/2026 07:30:00 +07:00", ReceiptFormatting.Timestamp(ReceiptSample.Create().IssuedAt));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void ReprintAndNullBuyer_AreReflectedInActualPdfText()
    {
        var invoice = ReceiptSample.Create() with { BuyerName = null, BuyerTaxCode = null, BuyerAddress = null };
        using var pdf = PdfDocument.Open(new QuestPdfInvoiceRenderer(Options).Render(invoice));
        var text = string.Join("\n", pdf.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.DoesNotContain("BẢN SAO", text);
        Assert.DoesNotContain("Người mua:", text);
        Assert.DoesNotContain("MST:", text);
    }

    [Fact]
    public void Renderer_HonorsCancellationBeforeGeneratingContent()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new QuestPdfInvoiceRenderer(Options).Render(ReceiptSample.Create(), cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => new EscPosReceiptRenderer(Options).Render(ReceiptSample.Create(), 58, cancellation.Token));
    }

    private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
    private static void ExportPreview(string name, byte[] bytes)
    {
        var directory = Environment.GetEnvironmentVariable("RECEIPT_PREVIEW_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name), bytes);
    }
    private static int FindRaster(byte[] bytes)
    {
        for (var i = 0; i + 8 < bytes.Length; i++)
            if (bytes[i] == 0x1d && bytes[i + 1] == 0x76 && bytes[i + 2] == 0x30 && bytes[i + 3] == 0)
                return i;
        return -1;
    }
}
