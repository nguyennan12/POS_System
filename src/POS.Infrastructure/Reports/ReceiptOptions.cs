namespace POS.Infrastructure.Reports;

public sealed class ReceiptOptions
{
    public const string SectionName = "Receipts";
    public int DefaultPaperWidth { get; set; } = 80;
    public string FontFamily { get; set; } = "Noto Sans Mono";
    public string RenderMode { get; set; } = "Raster";
    public int CodePage { get; set; } = 1258;
    // ESC t table number is printer-specific; 52 is Epson WPC1258.
    public int PrinterCodePage { get; set; } = 52;
}
