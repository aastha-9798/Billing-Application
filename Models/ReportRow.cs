namespace PlateBilling.Models;

/// <summary>
/// One line of a report: challan lines with the same plate type
/// and rate (and, in a detailed report, the same client and
/// challan no.) added together.
/// </summary>
public class ReportRow
{
    public int RowNumber { get; set; }

    // Detailed report only.
    public string ClientName { get; set; } = string.Empty;

    // Detailed report only.
    public int ChallanNo { get; set; }

    public string PlateTypeCode { get; set; } = string.Empty;

    public bool AreaBilling { get; set; }

    public string PlateLabel =>
        AreaBilling ? $"{PlateTypeCode} (area)" : PlateTypeCode;

    public int Quantity { get; set; }

    // Price per plate. For area billing: length × breadth × area rate.
    public decimal Rate { get; set; }

    public decimal Amount { get; set; }
}
