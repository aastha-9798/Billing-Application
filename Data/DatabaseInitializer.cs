using System.IO;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds default settings.
    /// </summary>
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


    // =========================================================
    // STARTUP CHECK
    // =========================================================

    /// <summary>
    /// Makes sure the database is present and undamaged before the app
    /// uses it. A missing database is restored from the newest good
    /// backup; a damaged one is moved aside (never deleted) and restored.
    /// Returns a message for the user, or null if nothing happened.
    /// </summary>
    public static string? EnsureDatabaseHealthy()
    {
        string path = AppPaths.DatabasePath;

        if (File.Exists(path))
        {
            // Only real damage moves the file aside; a database that is
            // merely locked or unreadable right now is left alone.
            if (BackupService.CheckHealth(path) != DatabaseHealth.Damaged)
            {
                return null;
            }

            string damagedPath = SetAside(path);
            var restored = BackupService.RestoreNewest();

            return restored == null
                ? "The database file was damaged and no usable backup was found, " +
                  "so the app will start with an empty database.\n\n" +
                  $"The damaged file was kept at:\n{damagedPath}"
                : "The database file was damaged. It has been restored from the backup " +
                  $"taken on {restored.TakenAt:dd MMM yyyy, hh:mm tt}.\n\n" +
                  "Entries made after that time are not included. " +
                  $"The damaged file was kept at:\n{damagedPath}";
        }

        var backup = BackupService.RestoreNewest();

        // No database and no backups: first run.
        return backup == null
            ? null
            : "The database file was missing. It has been restored from the backup " +
              $"taken on {backup.TakenAt:dd MMM yyyy, hh:mm tt}.\n\n" +
              "Entries made after that time are not included.";
    }

    private static string SetAside(string path)
    {
        string damagedPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            $"platebilling.damaged-{DateTime.Now:yyyy-MM-dd-HHmmss}.db");

        File.Move(path, damagedPath);

        foreach (string suffix in new[] { "-wal", "-shm" })
        {
            if (File.Exists(path + suffix))
            {
                File.Move(path + suffix, damagedPath + suffix);
            }
        }

        return damagedPath;
    }


    // =========================================================
    // ONE-TIME MOVE TO THE FIXED LOCATION
    // =========================================================

    /// <summary>
    /// Earlier versions created the database next to wherever the app
    /// was started from. On the first run with the fixed location, copy
    /// the old database there (the one with the most challans, if several
    /// are found). Runs only once; old files are left untouched.
    /// </summary>
    public static void ImportLegacyDatabase()
    {
        var config = AppConfig.Load();

        if (config.LegacyImportDone)
        {
            return;
        }

        if (!File.Exists(AppPaths.DatabasePath))
        {
            string? legacyPath = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory }
                .Select(folder => Path.GetFullPath(Path.Combine(folder, AppPaths.DatabaseFileName)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .OrderByDescending(p => BackupService.CountChallans(p) ?? -1)
                .FirstOrDefault();

            if (legacyPath != null)
            {
                BackupService.CopyDatabase(legacyPath, AppPaths.DatabasePath);
            }
        }

        config.LegacyImportDone = true;
        config.Save();
    }
}
