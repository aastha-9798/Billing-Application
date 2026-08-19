namespace PlateBilling.Models;

public class Challan
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int ClientId { get; set; }

    public string ChallanNo { get; set; } = string.Empty;
    public string PlateDescription { get; set; } = string.Empty;


    public int PlateTypeId { get; set; }

    public int Quantity { get; set; }

    public bool AreaBilling { get; set; }

    public decimal AppliedRate { get; set; }

    public decimal PlateLength { get; set; }

    public decimal PlateBreadth { get; set; }

    public decimal AppliedAreaRate { get; set; }

    public decimal TotalCost { get; set; }

    public Client Client { get; set; } = null!;

    public PlateType PlateType { get; set; } = null!;
}