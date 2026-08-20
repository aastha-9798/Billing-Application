using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ReportService
{
    public async Task<List<ReportRow>> GetReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? clientId = null)
    {
        using var db = new AppDbContext();

        var query = db.Challans
            .AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.PlateType)
            .Where(c =>
                c.Date >= startDate &&
                c.Date <= endDate);

        // null means "All Clients"
        if (clientId.HasValue)
        {
            query = query.Where(
                c => c.ClientId == clientId.Value);
        }

        var challans = await query.ToListAsync();

        challans = challans
            .OrderBy(c => c.ChallanNo)
            .ThenBy(c => c.EnteredAt)
            .ThenBy(c => c.Id)
            .ToList();

        var rows = new List<ReportRow>();

        foreach (var challan in challans)
        {
            decimal plateArea =
                challan.PlateLength *
                challan.PlateBreadth;

            decimal amount;

            if (challan.AreaBilling)
            {
                amount =
                    plateArea *
                    challan.AppliedAreaRate *
                    challan.Quantity;
            }
            else
            {
                amount =
                    challan.AppliedRate *
                    challan.Quantity;
            }

            rows.Add(new ReportRow
            {
                ClientName = challan.Client.Name,
                ChallanNo = challan.ChallanNo,
                PlateTypeCode = challan.PlateType.Code,
                Quantity = challan.Quantity,

                Rate = challan.AreaBilling
                    ? null
                    : challan.AppliedRate,

                AreaBilling = challan.AreaBilling,
                PlateArea = plateArea,
                Amount = Math.Round(amount, 2)
            });
        }

        return rows;
    }
}
