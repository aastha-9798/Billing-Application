using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using PlateBilling.Data;

namespace PlateBilling.Services;

public enum BackupTier
{
    Daily,
    Monthly,
    Yearly
}

public enum DatabaseHealth
{
    Healthy,
    Damaged,
    Unreadable
}

/// <summary>A backup file. Every backup is a complete copy of the database.</summary>
public sealed record BackupInfo(string Path, BackupTier Tier, DateTime Period, DateTime TakenAt, long Size);

/// <param name="FilePath">The backup written, or null if none was needed.</param>
/// <param name="Warning">Something the user should know about.</param>
public sealed record BackupResult(string? FilePath, string? Warning);

/// <summary>
/// Backups, kept in three folders inside the backup folder:
///
///   Daily\2026-10-08.db   one per day (replaced on every close); last 30 days
///   Monthly\2026-09.db    the last backup of each month; last 12 months
///   Yearly\2025.db        the last backup of each year; kept for ever
///
/// Every file is a complete copy of the database at that moment, so the
/// newest good file always holds all data up to when it was taken.
/// </summary>
public static class BackupService
{
    private const int KeepDailyDays = 30;
    private const int KeepMonths = 12;

    // A backup replacing today's file must keep at least this share of
    // its challans; otherwise it is saved alongside. Deleting a few wrong
    // entries never trips this; an emptied or wrong database does.
    private const double MinChallanShare = 0.5;

    private const string DailyFormat = "yyyy-MM-dd";
    private const string ExtraDailyFormat = "yyyy-MM-dd-HHmmss";
    private const string MonthlyFormat = "yyyy-MM";
    private const string YearlyFormat = "yyyy";

    public static string BackupFolder =>
        AppConfig.Load().BackupFolder ?? AppPaths.DefaultBackupFolder;


    // =========================================================
    // BACK UP
    // =========================================================

    /// <summary>
    /// Writes today's daily backup, first promoting finished months and
    /// years to Monthly / Yearly, then removes backups past retention.
    /// </summary>
    public static BackupResult BackUp()
    {
        if (!File.Exists(AppPaths.DatabasePath))
        {
            return new BackupResult(null, null);
        }

        if (!IsHealthy(AppPaths.DatabasePath))
        {
            return new BackupResult(null,
                "The database failed its integrity check, so it was not backed up " +
                "and your existing backups were left untouched.\n\n" +
                "Restart the app to restore it from the latest backup.");
        }

        var warnings = new List<string>();
        string root = ResolveFolder(warnings);

        MoveOldLayout(root);
        Promote(root);

        string dailyFolder = TierFolder(root, BackupTier.Daily);
        string target = Path.Combine(dailyFolder, Name(DateTime.Today, DailyFormat));

        if (File.Exists(target) && LostManyChallans(AppPaths.DatabasePath, target))
        {
            target = Path.Combine(dailyFolder, Name(DateTime.Now, ExtraDailyFormat));

            warnings.Add(
                "The database has far fewer challans than today's earlier backup, " +
                "so that backup was kept and this one was saved separately as " +
                $"\"{Path.GetFileName(target)}\".");
        }

        CopyDatabase(AppPaths.DatabasePath, target);
        Prune(root);

        return new BackupResult(
            target,
            warnings.Count == 0 ? null : string.Join("\n\n", warnings));
    }

    public static BackupResult BackUpIfNoneToday()
    {
        string today = Path.Combine(
            TierFolder(BackupFolder, BackupTier.Daily),
            Name(DateTime.Today, DailyFormat));

        return File.Exists(today)
            ? new BackupResult(null, null)
            : BackUp();
    }

    /// <summary>
    /// The chosen folder, or the default one if it is unavailable
    /// (e.g. USB drive removed).
    /// </summary>
    private static string ResolveFolder(List<string> warnings)
    {
        string folder = BackupFolder;

        try
        {
            Directory.CreateDirectory(folder);
            return folder;
        }
        catch (Exception ex) when (folder != AppPaths.DefaultBackupFolder)
        {
            warnings.Add(
                $"The backup folder \"{folder}\" is not available ({ex.Message}). " +
                $"The backup was saved to \"{AppPaths.DefaultBackupFolder}\" instead.");

            Directory.CreateDirectory(AppPaths.DefaultBackupFolder);
            return AppPaths.DefaultBackupFolder;
        }
    }


    // =========================================================
    // MONTHLY / YEARLY / RETENTION
    // =========================================================

    // The last backup of each finished month becomes that month's backup,
    // and the last backup of each finished year becomes that year's backup.
    private static void Promote(string root)
    {
        var all = List(root);
        DateTime thisMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        foreach (var month in all
                     .Where(b => b.Tier == BackupTier.Daily && b.Period < thisMonth)
                     .GroupBy(b => new DateTime(b.Period.Year, b.Period.Month, 1)))
        {
            string monthly = Path.Combine(
                TierFolder(root, BackupTier.Monthly), Name(month.Key, MonthlyFormat));

            if (!File.Exists(monthly))
            {
                CopyFile(month.MaxBy(b => b.TakenAt)!.Path, monthly);
            }
        }

        all = List(root);

        foreach (var year in all
                     .Where(b => b.Tier != BackupTier.Yearly && b.Period.Year < DateTime.Today.Year)
                     .GroupBy(b => b.Period.Year))
        {
            string yearly = Path.Combine(
                TierFolder(root, BackupTier.Yearly),
                Name(new DateTime(year.Key, 1, 1), YearlyFormat));

            if (!File.Exists(yearly))
            {
                CopyFile(year.MaxBy(b => b.TakenAt)!.Path, yearly);
            }
        }
    }

    private static void Prune(string root)
    {
        var all = List(root);

        var expired = all
            .Where(b => b.Tier == BackupTier.Daily &&
                        b.Period <= DateTime.Today.AddDays(-KeepDailyDays))
            .Concat(all
                .Where(b => b.Tier == BackupTier.Monthly)
                .OrderByDescending(b => b.Period)
                .Skip(KeepMonths));

        foreach (var backup in expired)
        {
            try
            {
                File.Delete(backup.Path);
            }
            catch (Exception)
            {
                // In use (e.g. by a sync client): try again next time.
            }
        }
    }

    // Earlier builds wrote platebilling-yyyy-MM-dd.db straight into the
    // backup folder; move those into Daily.
    private static void MoveOldLayout(string root)
    {
        foreach (string file in Directory.GetFiles(root, "platebilling-*.db"))
        {
            string datePart = Path.GetFileNameWithoutExtension(file)["platebilling-".Length..];

            if (DateTime.TryParseExact(
                    datePart, DailyFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                string target = Path.Combine(TierFolder(root, BackupTier.Daily), datePart + ".db");

                if (!File.Exists(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Move(file, target);
                }
            }
        }
    }


    // =========================================================
    // LIST
    // =========================================================

    /// <summary>
    /// All backups in the backup folder and the default folder,
    /// newest first.
    /// </summary>
    public static List<BackupInfo> ListAll()
    {
        return new[] { BackupFolder, AppPaths.DefaultBackupFolder }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(List)
            .OrderByDescending(b => b.TakenAt)
            .ToList();
    }

    public static BackupInfo? GetLatestBackup()
    {
        try
        {
            return List(BackupFolder).MaxBy(b => b.TakenAt);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<BackupInfo> List(string root)
    {
        var backups = new List<BackupInfo>();

        foreach (BackupTier tier in Enum.GetValues<BackupTier>())
        {
            string folder = TierFolder(root, tier);

            if (!Directory.Exists(folder))
            {
                continue;
            }

            string[] formats = tier switch
            {
                BackupTier.Daily => [DailyFormat, ExtraDailyFormat],
                BackupTier.Monthly => [MonthlyFormat],
                _ => [YearlyFormat]
            };

            // Only files named like a backup are ever listed (and so pruned).
            foreach (var file in new DirectoryInfo(folder).GetFiles("*.db"))
            {
                if (DateTime.TryParseExact(
                        Path.GetFileNameWithoutExtension(file.Name),
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime period))
                {
                    backups.Add(new BackupInfo(
                        file.FullName, tier, period.Date, file.LastWriteTime, file.Length));
                }
            }
        }

        return backups;
    }


    // =========================================================
    // RESTORE
    // =========================================================

    /// <summary>
    /// Copies the newest backup that passes the integrity check into
    /// place as the main database. Returns null if there is none.
    /// </summary>
    public static BackupInfo? RestoreNewest()
    {
        var backup = ListAll().FirstOrDefault(b => IsHealthy(b.Path));

        if (backup != null)
        {
            RestoreDatabaseFile(backup.Path);
        }

        return backup;
    }

    /// <summary>
    /// Replaces the main database with a backup while the app is running.
    /// The current database is saved first, so this can be undone.
    /// The app must restart afterwards.
    /// </summary>
    public static void Restore(BackupInfo backup)
    {
        if (!IsHealthy(backup.Path))
        {
            throw new InvalidOperationException(
                "This backup failed its integrity check and cannot be restored.");
        }

        // Keep the current state as an extra daily backup.
        if (File.Exists(AppPaths.DatabasePath))
        {
            var warnings = new List<string>();
            string root = ResolveFolder(warnings);

            CopyDatabase(
                AppPaths.DatabasePath,
                Path.Combine(TierFolder(root, BackupTier.Daily), Name(DateTime.Now, ExtraDailyFormat)));
        }

        // Close the app's pooled connections so the file can be replaced.
        SqliteConnection.ClearAllPools();

        RestoreDatabaseFile(backup.Path);
    }

    private static void RestoreDatabaseFile(string backupPath)
    {
        Directory.CreateDirectory(AppPaths.DataFolder);

        // Leftover journal files belong to the old database.
        File.Delete(AppPaths.DatabasePath + "-wal");
        File.Delete(AppPaths.DatabasePath + "-shm");

        CopyDatabase(backupPath, AppPaths.DatabasePath);

        // The app runs the database in WAL mode (as EF Core creates it).
        using var connection = Open(AppPaths.DatabasePath, SqliteOpenMode.ReadWrite);
        Execute(connection, "PRAGMA journal_mode = WAL;");
    }


    // =========================================================
    // CHECKS
    // =========================================================

    public static bool IsHealthy(string path) =>
        CheckHealth(path) == DatabaseHealth.Healthy;

    /// <summary>
    /// Healthy: opens, passes SQLite's integrity check and has the app's
    /// tables. Damaged: SQLite reports corruption, or it is not an app
    /// database. Unreadable: could not be checked (e.g. locked), which
    /// says nothing about the data.
    /// </summary>
    public static DatabaseHealth CheckHealth(string path)
    {
        const int SqliteError = 1;    // e.g. "no such table"
        const int SqliteCorrupt = 11;
        const int SqliteNotADb = 26;

        try
        {
            using var connection = Open(path, SqliteOpenMode.ReadWrite);

            using var check = connection.CreateCommand();
            check.CommandText = "PRAGMA quick_check;";

            if (check.ExecuteScalar() as string != "ok")
            {
                return DatabaseHealth.Damaged;
            }

            CountChallans(connection);
            return DatabaseHealth.Healthy;
        }
        catch (SqliteException ex) when (
            ex.SqliteErrorCode is SqliteError or SqliteCorrupt or SqliteNotADb)
        {
            return DatabaseHealth.Damaged;
        }
        catch (Exception)
        {
            return DatabaseHealth.Unreadable;
        }
    }

    public static long? CountChallans(string path)
    {
        try
        {
            using var connection = Open(path, SqliteOpenMode.ReadWrite);
            return CountChallans(connection);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static long CountChallans(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Challans;";

        return (long)command.ExecuteScalar()!;
    }

    private static bool LostManyChallans(string current, string previous)
    {
        long? now = CountChallans(current);
        long? before = CountChallans(previous);

        return now != null && before > 0 && now < before * MinChallanShare;
    }


    // =========================================================
    // FILES
    // =========================================================

    /// <summary>
    /// Copies a database using SQLite's backup API. Unlike a file copy,
    /// this includes recent changes still held in the -wal file and is
    /// safe while the app is using the database. The copy is a single
    /// self-contained file.
    /// </summary>
    public static void CopyDatabase(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        // Write to a temporary file first so a failed copy never
        // replaces a good file.
        string tempPath = targetPath + ".tmp";
        File.Delete(tempPath);

        using (var source = Open(sourcePath, SqliteOpenMode.ReadWrite))
        using (var target = Open(tempPath, SqliteOpenMode.ReadWriteCreate))
        {
            source.BackupDatabase(target);
            Execute(target, "PRAGMA journal_mode = DELETE;");
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    // Backup files are closed, self-contained files: a plain copy is
    // enough. The copy keeps the original's time stamp.
    private static void CopyFile(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        string tempPath = targetPath + ".tmp";
        File.Copy(sourcePath, tempPath, overwrite: true);
        File.Move(tempPath, targetPath, overwrite: true);
    }

    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = mode,
                // Release the file as soon as we are done.
                Pooling = false
            }.ToString());

        connection.Open();

        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string TierFolder(string root, BackupTier tier) =>
        Path.Combine(root, tier.ToString());

    private static string Name(DateTime date, string format) =>
        date.ToString(format, CultureInfo.InvariantCulture) + ".db";
}
