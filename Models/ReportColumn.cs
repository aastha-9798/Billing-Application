using System.Globalization;

namespace PlateBilling.Models;

/// <summary>
/// A report column. Used by the on-screen grid, the PDF and the printout,
/// so all three always show the same columns.
/// </summary>
/// <param name="Header">Column heading.</param>
/// <param name="Path">The <see cref="ReportRow"/> property shown.</param>
/// <param name="Width">Relative width.</param>
/// <param name="IsNumeric">Numbers are right-aligned.</param>
/// <param name="Format">Optional .NET format string, e.g. "N2".</param>
public sealed record ReportColumn(
    string Header,
    string Path,
    double Width,
    bool IsNumeric = false,
    string? Format = null)
{
    public static readonly ReportColumn SerialNo =
        new("S.No", nameof(ReportRow.RowNumber), 0.7);

    public static readonly ReportColumn Client =
        new("Client", nameof(ReportRow.ClientName), 2.0);

    public static readonly ReportColumn ChallanNo =
        new("Challan No.", nameof(ReportRow.ChallanNo), 1.5);

    public static readonly ReportColumn PlateType =
        new("Plate Type", nameof(ReportRow.PlateLabel), 1.4);

    public static readonly ReportColumn Quantity =
        new("Quantity", nameof(ReportRow.Quantity), 1.1, IsNumeric: true);

    public static readonly ReportColumn Rate =
        new("Rate", nameof(ReportRow.Rate), 1.3, IsNumeric: true, Format: "N2");

    public static readonly ReportColumn Amount =
        new("Amount", nameof(ReportRow.Amount), 1.6, IsNumeric: true, Format: "N2");

    public object? GetValue(ReportRow row)
    {
        return typeof(ReportRow).GetProperty(Path)?.GetValue(row);
    }

    public string GetText(ReportRow row)
    {
        object? value = GetValue(row);

        return Format == null
            ? value?.ToString() ?? string.Empty
            : string.Format(CultureInfo.CurrentCulture, $"{{0:{Format}}}", value);
    }
}
