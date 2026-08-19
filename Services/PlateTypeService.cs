using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class PlateTypeService
{
    public async Task<List<PlateType>> GetAllAsync()
    {
        using var db = new AppDbContext();

        return await db.PlateTypes
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<PlateType> CreateAsync(
        string code,
        decimal length,
        decimal breadth)
    {
        using var db = new AppDbContext();

        code = code.Trim();

        bool exists = await db.PlateTypes
            .AnyAsync(p => p.Code == code);

        if (exists)
        {
            throw new InvalidOperationException(
                "A plate type with this code already exists.");
        }

        var plateType = new PlateType
        {
            Code = code,
            Length = length,
            Breadth = breadth
        };

        db.PlateTypes.Add(plateType);

        await db.SaveChangesAsync();

        return plateType;
    }

    public async Task<PlateType> UpdateAsync(
        int id,
        string code,
        decimal length,
        decimal breadth)
    {
        using var db = new AppDbContext();

        code = code.Trim();

        var plateType = await db.PlateTypes
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plateType == null)
        {
            throw new InvalidOperationException(
                "Plate type was not found.");
        }

        bool duplicateCode = await db.PlateTypes
            .AnyAsync(p => p.Id != id && p.Code == code);

        if (duplicateCode)
        {
            throw new InvalidOperationException(
                "A plate type with this code already exists.");
        }

        plateType.Code = code;
        plateType.Length = length;
        plateType.Breadth = breadth;

        await db.SaveChangesAsync();

        return plateType;
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new AppDbContext();

        var plateType = await db.PlateTypes
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plateType == null)
        {
            throw new InvalidOperationException(
                "Plate type was not found.");
        }

        db.PlateTypes.Remove(plateType);

        await db.SaveChangesAsync();
    }
}