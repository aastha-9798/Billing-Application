using System.Windows;
using System.Windows.Input;
using PlateBilling.Services;

namespace PlateBilling.Views;

public partial class RestoreBackupWindow : Window
{
    // A row in the list: the backup plus its challan count, read when listed.
    private sealed record BackupRow(BackupInfo Backup, long? Challans)
    {
        public string ChallanText => Challans?.ToString("N0") ?? "unreadable";

        public string SizeText => $"{Backup.Size / 1048576.0:N1} MB";
    }

    public RestoreBackupWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            StatusText.Text = "Reading backups…";

            var rows = await Task.Run(() => BackupService.ListAll()
                .Select(b => new BackupRow(b, BackupService.CountChallans(b.Path)))
                .ToList());

            BackupsGrid.ItemsSource = rows;
            StatusText.Text = rows.Count == 0 ? "No backups found." : string.Empty;
        };
    }

    private BackupRow? SelectedRow => BackupsGrid.SelectedItem as BackupRow;

    private void BackupsGrid_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RestoreButton.IsEnabled = SelectedRow?.Challans != null;
    }

    private void BackupsGrid_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (RestoreButton.IsEnabled)
        {
            RestoreButton_Click(sender, e);
        }
    }

    private void RestoreButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (SelectedRow is not BackupRow row)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"Restore the {row.Backup.Tier.ToString().ToLowerInvariant()} backup taken on " +
            $"{row.Backup.TakenAt:dd MMM yyyy, hh:mm tt} ({row.ChallanText} challans)?\n\n" +
            "Everything entered after that time will be replaced. Your current data " +
            "is saved as a backup first, so this can be undone.\n\n" +
            "The app will restart.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            BackupService.Restore(row.Backup);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The backup could not be restored. Your data was not changed.\n\n{ex.Message}",
                "Restore Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        App.Restart();
    }
}
