using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ClientPlateRateService
{
    public async Task<List<ClientPlateRate>> GetAllAsync()
    {
        using var db = new AppDbContext();

        return await db.ClientPlateRates
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.PlateType)
            .OrderBy(r => r.Client.Name)
            .ThenBy(r => r.PlateType.Code)
            .ToListAsync();
    }

    public async Task<ClientPlateRate> CreateAsync(
        int clientId,
        int plateTypeId,
        decimal rate)
    {
        using var db = new AppDbContext();

        bool exists = await db.ClientPlateRates
            .AnyAsync(r =>
                r.ClientId == clientId &&
                r.PlateTypeId == plateTypeId);

        if (exists)
        {
            throw new InvalidOperationException(
                "A rate already exists for this client and plate type.");
        }

        var clientPlateRate = new ClientPlateRate
        {
            ClientId = clientId,
            PlateTypeId = plateTypeId,
            Rate = rate
        };

        db.ClientPlateRates.Add(clientPlateRate);

        await db.SaveChangesAsync();

        return await db.ClientPlateRates
            .Include(r => r.Client)
            .Include(r => r.PlateType)
            .FirstAsync(r => r.Id == clientPlateRate.Id);
    }

    public async Task<ClientPlateRate> UpdateAsync(
        int id,
        int clientId,
        int plateTypeId,
        decimal rate)
    {
        using var db = new AppDbContext();

        var clientPlateRate = await db.ClientPlateRates
            .FirstOrDefaultAsync(r => r.Id == id);

        if (clientPlateRate == null)
        {
            throw new InvalidOperationException(
                "The selected rate was not found.");
        }

        bool duplicate = await db.ClientPlateRates
            .AnyAsync(r =>
                r.Id != id &&
                r.ClientId == clientId &&
                r.PlateTypeId == plateTypeId);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A rate already exists for this client and plate type.");
        }

        clientPlateRate.ClientId = clientId;
        clientPlateRate.PlateTypeId = plateTypeId;
        clientPlateRate.Rate = rate;

        await db.SaveChangesAsync();

        return await db.ClientPlateRates
            .Include(r => r.Client)
            .Include(r => r.PlateType)
            .FirstAsync(r => r.Id == id);
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new AppDbContext();

        var rate = await db.ClientPlateRates
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rate == null)
        {
            throw new InvalidOperationException(
                "The selected rate was not found.");
        }

        db.ClientPlateRates.Remove(rate);

        await db.SaveChangesAsync();
    }

    public async Task<ClientPlateRate?> GetRateAsync(
            int clientId,
            int plateTypeId)
    {
        using var db = new AppDbContext();

        return await db.ClientPlateRates
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.PlateType)
            .FirstOrDefaultAsync(r =>
                r.ClientId == clientId &&
                r.PlateTypeId == plateTypeId);
    }
}