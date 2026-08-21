namespace PlateBilling.Models;

public class CumulativeReportRow
{
    public int RowNumber { get; set; }

    public string PlateTypeCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal? Rate { get; set; }

    public decimal Amount { get; set; }
}
