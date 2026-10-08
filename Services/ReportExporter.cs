using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PlateBilling.Models;

namespace PlateBilling.Services;

/// <summary>
/// Saves a report as PDF or Excel, or sends it to a printer. PDF and
/// print use the same <see cref="ReportLayout"/>.
/// </summary>
public static class ReportExporter
{
    private const string FontFamilyName = "Arial";

    public static string SuggestFileName(Report report)
    {
        string name =
            $"{report.Title} - {report.Type} {report.From:dd-MM-yyyy} to {report.To:dd-MM-yyyy}";

        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '-');
        }

        return name;
    }


    // =========================================================
    // EXCEL
    // =========================================================

    public static void SaveExcel(Report report, string filePath)
    {
        ReportExcelWriter.Save(report, filePath);
    }


    // =========================================================
    // PDF
    // =========================================================

    public static void SavePdf(Report report, string filePath)
    {
        var layout = new ReportLayout(report);
        var document = new PdfDocument();

        document.Info.Title = report.Title;

        for (int i = 0; i < layout.PageCount; i++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(ReportLayout.PageWidth);
            page.Height = XUnit.FromPoint(ReportLayout.PageHeight);

            using var gfx = XGraphics.FromPdfPage(page);

            layout.DrawPage(i, new PdfCanvas(gfx));
        }

        document.Save(filePath);
    }

    private sealed class PdfCanvas(XGraphics gfx) : IReportCanvas
    {
        private readonly Dictionary<TextStyle, XFont> _fonts = new();

        public void DrawRectangle(
            double x, double y, double width, double height,
            Color? fill, Color? stroke)
        {
            XBrush? brush = fill is Color f ? new XSolidBrush(ToX(f)) : null;
            XPen? pen = stroke is Color s ? new XPen(ToX(s), 0.5) : null;

            if (brush != null && pen != null)
            {
                gfx.DrawRectangle(pen, brush, x, y, width, height);
            }
            else if (brush != null)
            {
                gfx.DrawRectangle(brush, x, y, width, height);
            }
            else if (pen != null)
            {
                gfx.DrawRectangle(pen, x, y, width, height);
            }
        }

        public void DrawText(
            string text, TextStyle style,
            double x, double y, double width, double height,
            TextAlign align)
        {
            XFont font = GetFont(style);

            gfx.DrawString(
                Fit(text, font, width),
                font,
                new XSolidBrush(ToX(style.Color)),
                new XRect(x, y, width, height),
                align == TextAlign.Right
                    ? XStringFormats.CenterRight
                    : XStringFormats.CenterLeft);
        }

        private XFont GetFont(TextStyle style)
        {
            if (!_fonts.TryGetValue(style, out XFont? font))
            {
                // Unicode encoding is needed for the ₹ sign.
                font = new XFont(
                    FontFamilyName,
                    style.Size,
                    style.Bold ? XFontStyle.Bold : XFontStyle.Regular,
                    new XPdfFontOptions(PdfFontEncoding.Unicode));

                _fonts[style] = font;
            }

            return font;
        }

        private string Fit(string text, XFont font, double width)
        {
            if (gfx.MeasureString(text, font).Width <= width)
            {
                return text;
            }

            while (text.Length > 0 &&
                   gfx.MeasureString(text + "…", font).Width > width)
            {
                text = text[..^1];
            }

            return text + "…";
        }

        private static XColor ToX(Color c) => XColor.FromArgb(c.A, c.R, c.G, c.B);
    }


    // =========================================================
    // PRINT
    // =========================================================

    public static void Print(Report report, PrintDialog printDialog)
    {
        var layout = new ReportLayout(report);

        // Shrink to fit if the printer's paper is smaller than A4.
        double scale = Math.Min(1.0, Math.Min(
            printDialog.PrintableAreaWidth / ToDip(ReportLayout.PageWidth),
            printDialog.PrintableAreaHeight / ToDip(ReportLayout.PageHeight)));

        printDialog.PrintDocument(
            new ReportPaginator(layout, scale),
            report.Title);
    }

    // Points (1/72 inch) to WPF device-independent pixels (1/96 inch).
    private static double ToDip(double points) => points * 96 / 72;

    private sealed class ReportPaginator(ReportLayout layout, double scale)
        : DocumentPaginator
    {
        private readonly Size _pageSize = new(
            ToDip(ReportLayout.PageWidth) * scale,
            ToDip(ReportLayout.PageHeight) * scale);

        public override DocumentPage GetPage(int pageNumber)
        {
            var visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                // The layout works in points.
                double factor = ToDip(1) * scale;
                dc.PushTransform(new ScaleTransform(factor, factor));

                layout.DrawPage(pageNumber, new WpfCanvas(dc, visual));
            }

            var area = new Rect(_pageSize);

            return new DocumentPage(visual, _pageSize, area, area);
        }

        public override bool IsPageCountValid => true;

        public override int PageCount => layout.PageCount;

        public override Size PageSize
        {
            get => _pageSize;
            set { }
        }

        public override IDocumentPaginatorSource? Source => null;
    }

    private sealed class WpfCanvas(DrawingContext dc, Visual visual) : IReportCanvas
    {
        private readonly double _pixelsPerDip =
            VisualTreeHelper.GetDpi(visual).PixelsPerDip;

        public void DrawRectangle(
            double x, double y, double width, double height,
            Color? fill, Color? stroke)
        {
            dc.DrawRectangle(
                fill is Color f ? new SolidColorBrush(f) : null,
                stroke is Color s ? new Pen(new SolidColorBrush(s), 0.5) : null,
                new Rect(x, y, width, height));
        }

        public void DrawText(
            string text, TextStyle style,
            double x, double y, double width, double height,
            TextAlign align)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily(FontFamilyName),
                    FontStyles.Normal,
                    style.Bold ? FontWeights.Bold : FontWeights.Normal,
                    FontStretches.Normal),
                style.Size,
                new SolidColorBrush(style.Color),
                _pixelsPerDip)
            {
                MaxTextWidth = Math.Max(width, 1),
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis,
                TextAlignment = align == TextAlign.Right
                    ? TextAlignment.Right
                    : TextAlignment.Left
            };

            dc.DrawText(formatted, new Point(x, y + (height - formatted.Height) / 2));
        }
    }
}
