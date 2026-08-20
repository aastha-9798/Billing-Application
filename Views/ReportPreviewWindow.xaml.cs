using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        var printDialog =
            new System.Windows.Controls.PrintDialog();

        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        MessageBox.Show(
            "Printing will be connected to the formatted report in the next step.",
            "Print",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}