using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class SettingsViewModel : ObservableObject, IRefreshable
{
    [ObservableProperty]
    private string globalAreaRate = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SettingsViewModel()
    {
        _ = LoadAsync();

        RefreshBackupInfo();
    }


    // =========================================================
    // LOAD
    // =========================================================

    private async Task LoadAsync()
    {
        try
        {
            using var db = new AppDbContext();

            var settings = await db.ApplicationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings != null)
            {
                GlobalAreaRate = settings.GlobalAreaRate.ToString();
            }
        }
        catch (Exception)
        {
            StatusMessage = "Unable to load settings.";
        }
    }


    // =========================================================
    // SAVE
    // =========================================================

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = string.Empty;

        if (!decimal.TryParse(GlobalAreaRate, out decimal rate) || rate <= 0)
        {
            StatusMessage = "Enter a valid area rate greater than zero.";
            return;
        }

        try
        {
            using var db = new AppDbContext();

            var settings = await db.ApplicationSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                StatusMessage = "Settings record not found.";
                return;
            }

            settings.GlobalAreaRate = rate;

            await db.SaveChangesAsync();

            StatusMessage = "Settings saved successfully.";
        }
        catch (Exception)
        {
            StatusMessage = "Unable to save settings.";
        }
    }


    // =========================================================
    // DATA & BACKUPS
    // =========================================================

    public string DatabasePath => AppPaths.DatabasePath;

    [ObservableProperty]
    private string backupFolder = string.Empty;

    [ObservableProperty]
    private string lastBackupText = string.Empty;

    [ObservableProperty]
    private string backupStatus = string.Empty;

    [RelayCommand]
    private async Task BackUpNowAsync()
    {
        BackupStatus = "Backing up…";

        try
        {
            BackupResult result = await Task.Run(BackupService.BackUp);

            BackupStatus = result.Warning ?? "Backup saved.";
        }
        catch (Exception ex)
        {
            BackupStatus = $"Backup failed: {ex.Message}";
        }

        RefreshBackupInfo();
    }

    /// <summary>
    /// Saves the new backup folder and backs up there straight away,
    /// so a wrong choice shows up immediately.
    /// </summary>
    public async Task ChangeBackupFolderAsync(string folder)
    {
        var config = AppConfig.Load();
        config.BackupFolder = folder;
        config.Save();

        RefreshBackupInfo();

        await BackUpNowAsync();
    }

    public Task RefreshAsync()
    {
        RefreshBackupInfo();

        return Task.CompletedTask;
    }

    private void RefreshBackupInfo()
    {
        BackupFolder = BackupService.BackupFolder;

        var latest = BackupService.GetLatestBackup();

        LastBackupText = latest == null
            ? "No backups yet"
            : $"{latest.TakenAt:dd MMM yyyy, hh:mm tt}   ({latest.Tier}\\{Path.GetFileName(latest.Path)})";
    }
}
