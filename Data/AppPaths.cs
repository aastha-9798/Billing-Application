using System.IO;

namespace PlateBilling.Data;

/// <summary>
/// Fixed locations for the app's files, independent of where the app
/// is started from.
///
///   Database: %LocalAppData%\PlateBilling\platebilling.db
///   Settings: %LocalAppData%\PlateBilling\settings.json
///   Backups:  Documents\PlateBilling Backups (default; changeable in Settings)
///
/// The data folder can be overridden with the PLATEBILLING_DATA_DIR
/// environment variable (e.g. to test against a copy of the data).
/// </summary>
public static class AppPaths
{
    public const string DatabaseFileName = "platebilling.db";

    public static string DataFolder { get; } =
        Environment.GetEnvironmentVariable("PLATEBILLING_DATA_DIR")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PlateBilling");

    public static string DatabasePath { get; } =
        Path.Combine(DataFolder, DatabaseFileName);

    public static string ConfigPath { get; } =
        Path.Combine(DataFolder, "settings.json");

    public static string DefaultBackupFolder { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PlateBilling Backups");
}
