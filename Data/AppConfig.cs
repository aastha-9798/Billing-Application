using System.IO;
using System.Text.Json;

namespace PlateBilling.Data;

/// <summary>
/// Machine settings kept in a small JSON file next to the database
/// rather than inside it, so they are available before the database
/// is opened and survive if the database file is lost.
/// </summary>
public sealed class AppConfig
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    // Null means the default (AppPaths.DefaultBackupFolder).
    public string? BackupFolder { get; set; }

    // Set once the database from the old location (next to the app)
    // has been looked for, so a stale old copy is never imported later.
    public bool LegacyImportDone { get; set; }

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(AppPaths.ConfigPath))
            {
                return JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(AppPaths.ConfigPath)) ?? new AppConfig();
            }
        }
        catch (Exception)
        {
            // A damaged settings file falls back to defaults.
        }

        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(AppPaths.DataFolder);

        File.WriteAllText(
            AppPaths.ConfigPath,
            JsonSerializer.Serialize(this, JsonOptions));
    }
}
