using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace POS.Infrastructure.Reports;

internal static class ThermalReceiptDocument
{
    // 12 dots per receipt cell at 203 DPI; reserve 6 dots on each side.
    public static IDocument Create(IReadOnlyList<ReceiptLine> lines, int dots, string fontFamily) => Document.Create(document =>
        document.Page(page =>
        {
            page.ContinuousSize(dots * 72f / 203f);
            page.MarginHorizontal(6 * 72f / 203f);
            page.MarginVertical(4);
            page.DefaultTextStyle(x => x.FontFamily(fontFamily).FontSize(6.5f).LineHeight(1.35f));
            page.Content().Column(column =>
            {
                foreach (var line in lines)
                {
                    var container = column.Item();
                    if (line.Centered) container = container.AlignCenter();
                    var text = container.Text(line.Text);
                    if (line.Bold) text.Bold();
                }
            });
        }));
}
