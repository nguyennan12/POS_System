using System.Text;
using ESCPOS_NET.Emitters;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Invoices.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace POS.Infrastructure.Reports;

public sealed class EscPosReceiptRenderer(IOptions<ReceiptOptions> options) : IThermalReceiptRenderer
{
    public byte[] Render(InvoiceReceiptDto invoice, int paperWidth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dots = paperWidth switch { 58 => 384, 80 => 576, _ => throw new ArgumentOutOfRangeException(nameof(paperWidth)) };
        ReceiptFontRegistration.EnsureRegistered();
        try
        {
            var lines = ReceiptTextLayout.Build(invoice, paperWidth);
            var emitter = new EPSON();
            using var output = new MemoryStream();
            output.Write(emitter.Initialize());
            if (options.Value.RenderMode.Equals("Text", StringComparison.OrdinalIgnoreCase) && TryEncode(lines, out var encoded))
            {
                output.Write([0x1b, 0x74, (byte)options.Value.PrinterCodePage]);
                for (var i = 0; i < lines.Count; i++)
                {
                    output.Write([0x1b, 0x61, lines[i].Centered ? (byte)1 : (byte)0]);
                    output.Write([0x1b, 0x45, lines[i].Bold ? (byte)1 : (byte)0]);
                    output.Write(encoded[i]);
                    output.WriteByte(0x0a);
                }
            }
            else
            {
                output.Write([0x1b, 0x61, 0]);
                var images = ThermalReceiptDocument.Create(lines, dots, options.Value.FontFamily)
                    .GenerateImages(new ImageGenerationSettings { RasterDpi = 203, ImageFormat = ImageFormat.Png, UseTransparentBackground = false });
                foreach (var image in images)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Legacy raster emits GS v 0 directly. No printer, socket or serial I/O.
                    // The decoder only receives PNGs generated above, never uploaded images.
                    output.Write(emitter.BufferImage(image, maxWidth: dots, isLegacy: true));
                }
            }
            output.Write([0x1b, 0x64, 3]);
            output.Write(emitter.FullCut());
            return output.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        { throw new ReceiptRenderingException(); }
    }

    private bool TryEncode(IReadOnlyList<ReceiptLine> lines, out byte[][] encoded)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(options.Value.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        encoded = new byte[lines.Count][];
        try
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var text = options.Value.CodePage == 1258 ? ReceiptUnicode.Windows1258(lines[i].Text) : lines[i].Text;
                encoded[i] = encoding.GetBytes(text);
                if (ReceiptUnicode.Normalize(encoding.GetString(encoded[i])) != lines[i].Text)
                    return false;
            }
            return true;
        }
        catch (EncoderFallbackException) { return false; }
    }

}
