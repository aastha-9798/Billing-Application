using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ReportService
{
    // =========================================================
    // DETAILED REPORT
    // =========================================================

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
        int rowNumber = 1;

        foreach (var challan in challans)
        {
            decimal plateArea =
                challan.PlateLength * challan.PlateBreadth;

            decimal amount = challan.AreaBilling
                ? plateArea * challan.AppliedAreaRate * challan.Quantity
                : challan.AppliedRate * challan.Quantity;

            rows.Add(new ReportRow
            {
                RowNumber = rowNumber++,
                ClientName = challan.Client.Name,
                ChallanNo = challan.ChallanNo,
                PlateTypeCode = challan.PlateType.Code,
                Quantity = challan.Quantity,
                Rate = challan.AreaBilling ? null : challan.AppliedRate,
                AreaBilling = challan.AreaBilling,
                PlateArea = plateArea,
                Amount = Math.Round(amount, 2)
            });
        }

        return rows;
    }


    // =========================================================
    // CUMULATIVE REPORT
    // =========================================================

    public async Task<List<CumulativeReportRow>> GetCumulativeReportAsync(
        DateTime startDate,
        DateTime endDate,
        int clientId)
    {
        using var db = new AppDbContext();

        var challans = await db.Challans
            .AsNoTracking()
            .Include(c => c.PlateType)
            .Where(c =>
                c.ClientId == clientId &&
                c.Date >= startDate &&
                c.Date <= endDate)
            .ToListAsync();

        // Group by plate type, sum quantity and amount
        var groups = challans
            .GroupBy(c => c.PlateType.Code)
            .OrderBy(g => g.Key)
            .ToList();

        var rows = new List<CumulativeReportRow>();
        int rowNumber = 1;

        foreach (var group in groups)
        {
            int totalQty = group.Sum(c => c.Quantity);

            decimal totalAmount = group.Sum(c =>
            {
                decimal plateArea = c.PlateLength * c.PlateBreadth;
                return c.AreaBilling
                    ? Math.Round(plateArea * c.AppliedAreaRate * c.Quantity, 2)
                    : Math.Round(c.AppliedRate * c.Quantity, 2);
            });

            // Rate: use the first non-area-billing rate in the group,
            // or null if all entries are area-billed.
            decimal? rate = group
                .Where(c => !c.AreaBilling)
                .Select(c => (decimal?)c.AppliedRate)
                .FirstOrDefault();

            rows.Add(new CumulativeReportRow
            {
                RowNumber = rowNumber++,
                PlateTypeCode = group.Key,
                Quantity = totalQty,
                Rate = rate,
                Amount = Math.Round(totalAmount, 2)
            });
        }

        return rows;
    }
}
