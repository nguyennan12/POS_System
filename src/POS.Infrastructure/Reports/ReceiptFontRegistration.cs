using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace POS.Infrastructure.Reports;

public static class ReceiptFontRegistration
{
    private static readonly Lazy<bool> Registration = new(() =>
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = true;
        var assembly = typeof(ReceiptFontRegistration).Assembly;
        foreach (var weight in new[] { "Regular", "Bold" })
        {
            using var font = assembly.GetManifestResourceStream(
                $"POS.Infrastructure.Reports.Fonts.NotoSansMono-{weight}.ttf")
                ?? throw new InvalidOperationException("Embedded receipt font is missing.");
            FontManager.RegisterFontFromStream(font);
        }
        return true;
    });

    public static void EnsureRegistered() => _ = Registration.Value;
}
