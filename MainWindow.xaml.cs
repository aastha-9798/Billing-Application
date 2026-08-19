using System.Windows;
using PlateBilling.Views;

namespace PlateBilling;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Open Challan Entry when the application starts.
        ShowChallanEntry();
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    private void ShowChallanEntry()
    {
        MainContent.Content = new ChallanView();
    }


    private void ShowBillGeneration()
    {
        MainContent.Content = new BillGenerationView();
    }


    private void ShowClients()
    {
        MainContent.Content = new ClientsView();
    }


    private void ShowPlateTypes()
    {
        MainContent.Content = new PlateTypesView();
    }


    private void ShowRates()
    {
        MainContent.Content = new RatesView();
    }


    private void ShowReports()
    {
        MainContent.Content = new ReportsView();
    }


    private void ShowSettings()
    {
        MainContent.Content = new SettingsView();
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


    private void BillGenerationButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowBillGeneration();
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