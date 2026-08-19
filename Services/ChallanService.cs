using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ChallanService
{
    // =========================================================
    // GET ALL BILLS
    // =========================================================

    public async Task<List<Challan>> GetAllAsync()
    {
        using var db = new AppDbContext();

        return await db.Challans
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.PlateType)
            .OrderByDescending(b => b.Id)
            .ToListAsync();
    }


    // =========================================================
    // CREATE BILL / CHALLAN ENTRY
    // =========================================================

    public async Task<Challan> CreateAsync(
        DateTime date,
        int clientId,
        string challanNo,
        string plateDescription,
        int plateTypeId,
        int quantity,
        bool areaBilling,
        decimal appliedRate,
        decimal plateLength,
        decimal plateBreadth,
        decimal appliedAreaRate,
        decimal totalCost)
    {
        using var db = new AppDbContext();

        var challan = new Challan
        {
            Date = date,
            ClientId = clientId,
            ChallanNo = challanNo.Trim(),
            PlateDescription = plateDescription.Trim(),
            PlateTypeId = plateTypeId,
            Quantity = quantity,
            AreaBilling = areaBilling,
            AppliedRate = appliedRate,
            PlateLength = plateLength,
            PlateBreadth = plateBreadth,
            AppliedAreaRate = appliedAreaRate,
            TotalCost = totalCost
        };

        db.Challans.Add(challan);

        await db.SaveChangesAsync();

        return await db.Challans
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.PlateType)
            .FirstAsync(b => b.Id == challan.Id);
    }


    // =========================================================
    // UPDATE EXISTING BILL / CHALLAN ENTRY
    // =========================================================

    public async Task<Challan> UpdateAsync(
        int id,
        DateTime date,
        int clientId,
        string challanNo,
        string plateDescription,
        int plateTypeId,
        int quantity,
        bool areaBilling,
        decimal appliedRate,
        decimal plateLength,
        decimal plateBreadth,
        decimal appliedAreaRate,
        decimal totalCost)
    {
        using var db = new AppDbContext();

        var challan = await db.Challans
            .FirstOrDefaultAsync(b => b.Id == id);

        if (challan == null)
        {
            throw new InvalidOperationException(
                "The challan entry could not be found.");
        }

        challan.Date = date;
        challan.ClientId = clientId;
        challan.ChallanNo = challanNo.Trim();
        challan.PlateDescription = plateDescription.Trim();
        challan.PlateTypeId = plateTypeId;
        challan.Quantity = quantity;
        challan.AreaBilling = areaBilling;
        challan.AppliedRate = appliedRate;
        challan.PlateLength = plateLength;
        challan.PlateBreadth = plateBreadth;
        challan.AppliedAreaRate = appliedAreaRate;
        challan.TotalCost = totalCost;

        await db.SaveChangesAsync();

        return await db.Challans
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.PlateType)
            .FirstAsync(b => b.Id == id);
    }


    // =========================================================
    // DELETE EXISTING BILL / CHALLAN ENTRY
    // =========================================================

    public async Task DeleteAsync(int id)
    {
        using var db = new AppDbContext();

        var challan = await db.Challans
            .FirstOrDefaultAsync(b => b.Id == id);

        if (challan == null)
        {
            throw new InvalidOperationException(
                "The challan entry could not be found.");
        }

        db.Challans.Remove(challan);

        await db.SaveChangesAsync();
    }
}