namespace PlateBilling.Models;

public class ClientPlateRate
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int PlateTypeId { get; set; }

    public decimal Rate { get; set; }

    public Client Client { get; set; } = null!;

    public PlateType PlateType { get; set; } = null!;
}