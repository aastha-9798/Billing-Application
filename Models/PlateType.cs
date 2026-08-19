namespace PlateBilling.Models;

public class PlateType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public decimal Length { get; set; }

    public decimal Breadth { get; set; }
}