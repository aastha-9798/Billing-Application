using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;

namespace PlateBilling.Services;

public class ApplicationSettingsService
{
    public async Task<decimal> GetGlobalAreaRateAsync()
    {
        using var db = new AppDbContext();

        var settings = await db.ApplicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            throw new InvalidOperationException(
                "Application settings have not been initialized.");
        }

        return settings.GlobalAreaRate;
    }
}
