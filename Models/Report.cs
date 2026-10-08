namespace PlateBilling.Models;

/// <summary>
/// A generated report, ready to show, print or save as PDF.
/// </summary>
public class Report
{
    public required ReportType Type { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required IReadOnlyList<ReportColumn> Columns { get; init; }

    public required IReadOnlyList<ReportRow> Rows { get; init; }

    public int TotalQuantity => Rows.Sum(r => r.Quantity);

    public decimal GrandTotal => Rows.Sum(r => r.Amount);
}
