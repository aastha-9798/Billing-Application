namespace PlateBilling.Models;

public class ReportRow
{
    public int RowNumber { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int ChallanNo { get; set; }

    public string PlateTypeCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal? Rate { get; set; }

    public bool AreaBilling { get; set; }

    public decimal PlateArea { get; set; }

    public decimal Amount { get; set; }
}