using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PlateBilling.ViewModels;

namespace PlateBilling.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public SettingsView()
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }


    // =========================================================
    // DATA & BACKUPS
    // =========================================================

    private async void ChangeBackupFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose Backup Folder",
            InitialDirectory = Directory.Exists(_viewModel.BackupFolder)
                ? _viewModel.BackupFolder
                : string.Empty
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            await _viewModel.ChangeBackupFolderAsync(dialog.FolderName);
        }
    }

    private void RestoreBackup_Click(
        object sender,
        RoutedEventArgs e)
    {
        new RestoreBackupWindow { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private void OpenDatabaseFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenInExplorer(Path.GetDirectoryName(_viewModel.DatabasePath));
    }

    private void OpenBackupFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenInExplorer(_viewModel.BackupFolder);
    }

    private static void OpenInExplorer(string? folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show(
                "This folder does not exist yet.",
                "Open Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        Process.Start("explorer.exe", $"\"{folder}\"");
    }
}
