using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;

namespace PlateBilling.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string globalAreaRate = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SettingsViewModel()
    {
        _ = LoadAsync();
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
}
