using System.Windows.Media;
using PlateBilling.Models;

namespace PlateBilling.Services;

public enum TextAlign
{
    Left,
    Right
}

public readonly record struct TextStyle(double Size, bool Bold, Color Color);

/// <summary>
/// Drawing surface for a report page. Units are points (1/72 inch).
/// </summary>
public interface IReportCanvas
{
    void DrawRectangle(
        double x, double y, double width, double height,
        Color? fill, Color? stroke);

    /// <summary>
    /// Draws one line of text, vertically centred in the box and
    /// shortened with "…" if it does not fit.
    /// </summary>
    void DrawText(
        string text, TextStyle style,
        double x, double y, double width, double height,
        TextAlign align);
}

/// <summary>
/// Lays a report out on A4 pages. The same layout is drawn to PDF and
/// to the printer, so both always look the same.
/// </summary>
public sealed class ReportLayout
{
    public const double PageWidth = 595;   // A4, points
    public const double PageHeight = 842;

    private const double Margin = 40;
    private const double ContentWidth = PageWidth - 2 * Margin;
    private const double HeaderHeight = 60;
    private const double TableHeaderHeight = 24;
    private const double RowHeight = 21;
    private const double TotalsHeight = 60;
    private const double FooterHeight = 24;
    private const double CellPadding = 6;

    private static readonly int RowsPerPage = (int)(
        (PageHeight - 2 * Margin - HeaderHeight - TableHeaderHeight - TotalsHeight - FooterHeight)
        / RowHeight);

    private static readonly Color Ink = Color.FromRgb(0x17, 0x20, 0x33);
    private static readonly Color Muted = Color.FromRgb(0x66, 0x70, 0x85);
    private static readonly Color Rule = Color.FromRgb(0xD0, 0xD5, 0xDD);
    private static readonly Color Shade = Color.FromRgb(0xF2, 0xF4, 0xF7);
    private static readonly Color Stripe = Color.FromRgb(0xFA, 0xFB, 0xFC);

    private static readonly TextStyle TitleStyle = new(18, true, Ink);
    private static readonly TextStyle SubtitleStyle = new(10, false, Muted);
    private static readonly TextStyle HeaderStyle = new(10, true, Ink);
    private static readonly TextStyle CellStyle = new(10, false, Ink);
    private static readonly TextStyle TotalStyle = new(10.5, true, Ink);
    private static readonly TextStyle GrandTotalStyle = new(13, true, Ink);
    private static readonly TextStyle FooterStyle = new(8.5, false, Muted);

    private readonly Report _report;
    private readonly List<List<ReportRow>> _pages;
    private readonly double[] _columnWidths;

    public ReportLayout(Report report)
    {
        _report = report;

        _pages = report.Rows
            .Chunk(RowsPerPage)
            .Select(chunk => chunk.ToList())
            .ToList();

        if (_pages.Count == 0)
        {
            _pages.Add([]);
        }

        double totalWeight = report.Columns.Sum(c => c.Width);

        _columnWidths = report.Columns
            .Select(c => ContentWidth * c.Width / totalWeight)
            .ToArray();
    }

    public int PageCount => _pages.Count;

    public void DrawPage(int pageIndex, IReportCanvas canvas)
    {
        double y = Margin;

        y = DrawHeader(canvas, y);
        y = DrawTableHeader(canvas, y);

        bool stripe = false;

        foreach (var row in _pages[pageIndex])
        {
            canvas.DrawRectangle(
                Margin, y, ContentWidth, RowHeight,
                stripe ? Stripe : null, Rule);

            DrawCells(canvas, y, RowHeight, CellStyle, c => c.GetText(row));

            y += RowHeight;
            stripe = !stripe;
        }

        if (pageIndex == _pages.Count - 1)
        {
            DrawTotals(canvas, y);
        }

        DrawFooter(canvas, pageIndex);
    }


    // =========================================================
    // SECTIONS
    // =========================================================

    private double DrawHeader(IReportCanvas canvas, double y)
    {
        canvas.DrawText(
            _report.Title, TitleStyle,
            Margin, y, ContentWidth, 24, TextAlign.Left);

        canvas.DrawText(
            _report.Subtitle, SubtitleStyle,
            Margin, y + 26, ContentWidth, 16, TextAlign.Left);

        return y + HeaderHeight;
    }

    private double DrawTableHeader(IReportCanvas canvas, double y)
    {
        canvas.DrawRectangle(
            Margin, y, ContentWidth, TableHeaderHeight,
            Shade, Rule);

        DrawCells(canvas, y, TableHeaderHeight, HeaderStyle, c => c.Header);

        return y + TableHeaderHeight;
    }

    private void DrawTotals(IReportCanvas canvas, double y)
    {
        canvas.DrawRectangle(
            Margin, y, ContentWidth, RowHeight + 2,
            Shade, Rule);

        canvas.DrawText(
            "Total", TotalStyle,
            Margin + CellPadding, y, ContentWidth / 2, RowHeight + 2,
            TextAlign.Left);

        DrawCellAt(canvas, ReportColumn.Quantity, y, RowHeight + 2, TotalStyle,
            _report.TotalQuantity.ToString("N0"));

        DrawCellAt(canvas, ReportColumn.Amount, y, RowHeight + 2, TotalStyle,
            _report.GrandTotal.ToString("N2"));

        canvas.DrawText(
            $"Grand Total:  ₹ {_report.GrandTotal:N2}", GrandTotalStyle,
            Margin, y + RowHeight + 14, ContentWidth, 20,
            TextAlign.Right);
    }

    private void DrawFooter(IReportCanvas canvas, int pageIndex)
    {
        double y = PageHeight - Margin - 12;

        canvas.DrawText(
            $"Generated {DateTime.Now:dd MMM yyyy, hh:mm tt}", FooterStyle,
            Margin, y, ContentWidth, 12, TextAlign.Left);

        canvas.DrawText(
            $"Page {pageIndex + 1} of {_pages.Count}", FooterStyle,
            Margin, y, ContentWidth, 12, TextAlign.Right);
    }


    // =========================================================
    // CELLS
    // =========================================================

    private void DrawCells(
        IReportCanvas canvas,
        double y,
        double height,
        TextStyle style,
        Func<ReportColumn, string> getText)
    {
        double x = Margin;

        for (int i = 0; i < _report.Columns.Count; i++)
        {
            var column = _report.Columns[i];

            canvas.DrawText(
                getText(column), style,
                x + CellPadding, y, _columnWidths[i] - 2 * CellPadding, height,
                column.IsNumeric ? TextAlign.Right : TextAlign.Left);

            x += _columnWidths[i];
        }
    }

    private void DrawCellAt(
        IReportCanvas canvas,
        ReportColumn column,
        double y,
        double height,
        TextStyle style,
        string text)
    {
        int index = _report.Columns.ToList().IndexOf(column);

        if (index < 0)
        {
            return;
        }

        double x = Margin + _columnWidths.Take(index).Sum();

        canvas.DrawText(
            text, style,
            x + CellPadding, y, _columnWidths[index] - 2 * CellPadding, height,
            TextAlign.Right);
    }
}
