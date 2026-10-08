using System.Windows;
using System.Windows.Controls;
using PlateBilling.Services;
using PlateBilling.ViewModels;
using PlateBilling.Views;

namespace PlateBilling;

public partial class MainWindow : Window
{
    // One instance per screen for the whole session, so a screen keeps
    // its state (form fields, filters, selection) when you come back.
    private readonly Dictionary<Type, UserControl> _views = new();

    public MainWindow()
    {
        InitializeComponent();

        // Open Challan Entry when the application starts.
        ShowChallanEntry();
    }


    // Back up the day's entries when the app is closed.
    protected override void OnClosed(EventArgs e)
    {
        if (!App.SkipExitBackup)
        {
            App.RunBackup(BackupService.BackUp);
        }

        base.OnClosed(e);
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    private void Show<TView>() where TView : UserControl, new()
    {
        if (_views.TryGetValue(typeof(TView), out var view))
        {
            // Returning to a screen: pick up changes made on other screens.
            if (view.DataContext is IRefreshable refreshable)
            {
                _ = refreshable.RefreshAsync();
            }
        }
        else
        {
            view = new TView();
            _views[typeof(TView)] = view;
        }

        MainContent.Content = view;
    }


    private void ShowChallanEntry()
    {
        Show<ChallanView>();
    }


    private void ShowClients()
    {
        Show<ClientsView>();
    }


    private void ShowPlateTypes()
    {
        Show<PlateTypesView>();
    }


    private void ShowRates()
    {
        Show<RatesView>();
    }


    private void ShowReports()
    {
        Show<ReportsView>();
    }


    private void ShowSettings()
    {
        Show<SettingsView>();
    }


    // =========================================================
    // BUTTON EVENTS
    // =========================================================

    private void ChallanEntryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowChallanEntry();
    }


    private void ClientsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowClients();
    }


    private void PlateTypesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowPlateTypes();
    }


    private void RatesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowRates();
    }


    private void ReportsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowReports();
    }


    private void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowSettings();
    }
}