namespace PlateBilling.Models;

public class ReportRow
{
    public string ClientName { get; set; } = string.Empty;

    public int ChallanNo { get; set; }

    public string PlateTypeCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // Null for area-billing rows because the report
    // does not show a client rate in that case.
    public decimal? Rate { get; set; }

    public bool AreaBilling { get; set; }

    public decimal PlateArea { get; set; }

    public decimal Amount { get; set; }
}