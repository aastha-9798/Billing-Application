using System.Diagnostics;
using PlateBilling.Data;
using PlateBilling.Services;
using System.Windows;

namespace PlateBilling;

public partial class App : Application
{
    // Set when the app restarts itself after a restore, so the restored
    // database is not immediately backed up over today's backup.
    public static bool SkipExitBackup { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DatabaseInitializer.ImportLegacyDatabase();

        // Restore from backup if the database is missing or damaged.
        string? recovery = DatabaseInitializer.EnsureDatabaseHealthy();

        if (recovery != null)
        {
            MessageBox.Show(
                recovery,
                "Database Restored",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        // Before migrations, so there is always a copy from before
        // any schema change.
        RunBackup(BackupService.BackUpIfNoneToday);

        DatabaseInitializer.Initialize();
    }

    /// <summary>
    /// Starts a fresh copy of the app and closes this one (after a
    /// restore, so every screen loads the restored data).
    /// </summary>
    public static void Restart()
    {
        SkipExitBackup = true;

        Process.Start(Environment.ProcessPath!);
        Current.Shutdown();
    }


    // =========================================================
    // BACKUP
    // =========================================================

    /// <summary>
    /// Runs a backup and tells the user about anything unusual.
    /// A failed backup never stops the app.
    /// </summary>
    public static void RunBackup(Func<BackupResult> backup)
    {
        try
        {
            BackupResult result = backup();

            if (result.Warning != null)
            {
                MessageBox.Show(
                    result.Warning,
                    "Backup",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The automatic backup could not be made.\n\n{ex.Message}\n\n" +
                "Your data is safe; you can back up from Settings.",
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
