using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;

namespace PlateBilling.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var db = new AppDbContext();

        db.Database.Migrate();

        if (!db.ApplicationSettings.Any())
        {
            db.ApplicationSettings.Add(new ApplicationSetting
            {
                GlobalAreaRate = 470m
            });

            db.SaveChanges();
        }
    }
}