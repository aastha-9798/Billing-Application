using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using PlateBilling.Models;
namespace PlateBilling.Views;

public partial class ReportPreviewWindow : Window
{
    private readonly List<ReportRow> _rows;
    private readonly string _reportPeriod;
    private readonly string _clientName;
    private readonly decimal _grandTotal;

    public ReportPreviewWindow(
        IEnumerable<ReportRow> rows,
        string reportPeriod,
        string clientName,
        decimal grandTotal)
    {
        InitializeComponent();

        _rows = rows.ToList();
        _reportPeriod = reportPeriod;
        _clientName = clientName;
        _grandTotal = grandTotal;

        ReportGrid.ItemsSource = _rows;

        PeriodText.Text =
            $"Period: {_reportPeriod}";

        ClientText.Text =
            $"Client: {_clientName}";

        GrandTotalText.Text =
            $"₹{_grandTotal:N2}";
    }

    private void SavePdfButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Save Report as PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            FileName =
                $"Challan Report {_reportPeriod.Replace(" ", "-").Replace("/", "-")}"
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            CreatePdf(saveDialog.FileName);

            MessageBox.Show(
                "Report PDF saved successfully.",
                "PDF Saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to save the PDF.\n\n{ex.Message}",
                "PDF Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CreatePdf(string filePath)
    {
        var document = new PdfDocument();

        const double left = 40;
        const double top = 40;

        var pages = SplitIntoPages(_rows, 22);

        for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var page = document.AddPage();

            page.Size = PdfSharpCore.PageSize.A4;

            using var graphics =
                XGraphics.FromPdfPage(page);

            var titleFont =
                new XFont("Arial", 18, XFontStyle.Bold);

            var normalFont =
                new XFont("Arial", 9, XFontStyle.Regular);

            var headerFont =
                new XFont("Arial", 9, XFontStyle.Bold);

            var totalFont =
                new XFont("Arial", 12, XFontStyle.Bold);

            double y = top;

            graphics.DrawString(
                "Challan Report",
                titleFont,
                XBrushes.Black,
                new XPoint(left, y));

            y += 24;

            graphics.DrawString(
                $"Period: {_reportPeriod}",
                normalFont,
                XBrushes.Black,
                new XPoint(left, y));

            y += 15;

            graphics.DrawString(
                $"Client: {_clientName}",
                normalFont,
                XBrushes.Black,
                new XPoint(left, y));

            y += 25;

            double[] widths = { 85, 70, 65, 55, 70, 85 };

            string[] headers =
            {
            "Client",
            "Challan",
            "Plate",
            "Qty",
            "Rate",
            "Amount"
        };

            double x = left;

            for (int i = 0; i < headers.Length; i++)
            {
                graphics.DrawRectangle(
                    XPens.Black,
                    x,
                    y,
                    widths[i],
                    22);

                graphics.DrawString(
                    headers[i],
                    headerFont,
                    XBrushes.Black,
                    new XRect(
                        x + 3,
                        y + 5,
                        widths[i] - 6,
                        15),
                    XStringFormats.TopLeft);

                x += widths[i];
            }

            y += 22;

            foreach (var row in pages[pageIndex])
            {
                x = left;

                string[] values =
                {
                row.ClientName,
                row.ChallanNo.ToString(),
                row.PlateTypeCode,
                row.Quantity.ToString(),
                row.Rate.HasValue
                    ? $"₹{row.Rate.Value:N2}"
                    : "-",
                $"₹{row.Amount:N2}"
            };

                for (int i = 0; i < values.Length; i++)
                {
                    graphics.DrawRectangle(
                        XPens.LightGray,
                        x,
                        y,
                        widths[i],
                        20);

                    graphics.DrawString(
                        values[i],
                        normalFont,
                        XBrushes.Black,
                        new XRect(
                            x + 3,
                            y + 4,
                            widths[i] - 6,
                            15),
                        XStringFormats.TopLeft);

                    x += widths[i];
                }

                y += 20;
            }

            if (pageIndex == pages.Count - 1)
            {
                graphics.DrawString(
                    $"Grand Total: ₹{_grandTotal:N2}",
                    totalFont,
                    XBrushes.Black,
                    new XPoint(
                        left,
                        page.Height - 45));
            }
        }

        document.Save(filePath);
    }

    private static List<List<ReportRow>> SplitIntoPages(
        List<ReportRow> rows,
        int rowsPerPage)
    {
        var pages = new List<List<ReportRow>>();

        for (int i = 0; i < rows.Count; i += rowsPerPage)
        {
            pages.Add(
                rows
                    .Skip(i)
                    .Take(rowsPerPage)
                    .ToList());
        }

        if (pages.Count == 0)
        {
            pages.Add(new List<ReportRow>());
        }

        return pages;
    }

    private void PrintButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var printDialog = new System.Windows.Controls.PrintDialog();

        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            PrintReport(printDialog);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to print the report.\n\n{ex.Message}",
                "Print Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PrintReport(
        System.Windows.Controls.PrintDialog printDialog)
    {
        // Use same column layout as PDF export.
        double[] widths = { 130, 90, 90, 70, 90, 100 };
        string[] headers =
        {
            "Client", "Challan", "Plate", "Qty", "Rate", "Amount"
        };

        const double left = 40;
        const double top = 40;
        const double rowHeight = 22;
        const double headerRowHeight = 24;
        const int rowsPerPage = 28;

        double pageWidth = printDialog.PrintableAreaWidth;
        double pageHeight = printDialog.PrintableAreaHeight;

        var pages = SplitIntoPages(_rows, rowsPerPage);

        var titleTypeface = new Typeface("Arial");
        var normalTypeface = new Typeface("Arial");

        for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                double y = top;

                // -------------------------------------------------
                // Title
                // -------------------------------------------------

                var titleText = new FormattedText(
                    "Challan Report",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(
                        new FontFamily("Arial"),
                        FontStyles.Normal,
                        FontWeights.Bold,
                        FontStretches.Normal),
                    18,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                dc.DrawText(titleText, new Point(left, y));
                y += 26;

                // Period + client sub-heading
                foreach (string line in new[]
                {
                    $"Period: {_reportPeriod}",
                    $"Client: {_clientName}"
                })
                {
                    var subText = new FormattedText(
                        line,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        normalTypeface,
                        11,
                        Brushes.DimGray,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    dc.DrawText(subText, new Point(left, y));
                    y += 16;
                }

                y += 10;

                // -------------------------------------------------
                // Column headers
                // -------------------------------------------------

                double x = left;

                for (int i = 0; i < headers.Length; i++)
                {
                    dc.DrawRectangle(
                        new SolidColorBrush(
                            Color.FromRgb(0xF8, 0xF9, 0xFC)),
                        new Pen(Brushes.LightGray, 0.5),
                        new Rect(x, y, widths[i], headerRowHeight));

                    var headerText = new FormattedText(
                        headers[i],
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(
                            new FontFamily("Arial"),
                            FontStyles.Normal,
                            FontWeights.Bold,
                            FontStretches.Normal),
                        10,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    dc.DrawText(
                        headerText,
                        new Point(x + 5, y + 5));

                    x += widths[i];
                }

                y += headerRowHeight;

                // -------------------------------------------------
                // Data rows
                // -------------------------------------------------

                bool alternate = false;

                foreach (var row in pages[pageIndex])
                {
                    x = left;

                    var rowBg = alternate
                        ? new SolidColorBrush(
                            Color.FromRgb(0xF8, 0xF9, 0xFC))
                        : Brushes.White;

                    alternate = !alternate;

                    string[] values =
                    {
                        row.ClientName,
                        row.ChallanNo.ToString(),
                        row.PlateTypeCode,
                        row.Quantity.ToString(),
                        row.Rate.HasValue
                            ? $"₹{row.Rate.Value:N2}"
                            : "-",
                        $"₹{row.Amount:N2}"
                    };

                    for (int i = 0; i < values.Length; i++)
                    {
                        dc.DrawRectangle(
                            rowBg,
                            new Pen(Brushes.LightGray, 0.5),
                            new Rect(x, y, widths[i], rowHeight));

                        var cellText = new FormattedText(
                            values[i],
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            normalTypeface,
                            10,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                        dc.DrawText(
                            cellText,
                            new Point(x + 5, y + 4));

                        x += widths[i];
                    }

                    y += rowHeight;
                }

                // -------------------------------------------------
                // Grand total on last page
                // -------------------------------------------------

                if (pageIndex == pages.Count - 1)
                {
                    y += 12;

                    var totalText = new FormattedText(
                        $"Grand Total:  ₹{_grandTotal:N2}",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(
                            new FontFamily("Arial"),
                            FontStyles.Normal,
                            FontWeights.Bold,
                            FontStretches.Normal),
                        13,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    dc.DrawText(
                        totalText,
                        new Point(left, y));
                }

                // -------------------------------------------------
                // Page number
                // -------------------------------------------------

                var pageNumText = new FormattedText(
                    $"Page {pageIndex + 1} of {pages.Count}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    normalTypeface,
                    9,
                    Brushes.Gray,
                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                dc.DrawText(
                    pageNumText,
                    new Point(
                        left,
                        pageHeight - 30));
            }

            printDialog.PrintVisual(
                visual,
                $"Challan Report — Page {pageIndex + 1}");
        }
    }
}