using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlateBilling.ViewModels;

namespace PlateBilling.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();

        DataContext = new ReportsViewModel();

        PreviewKeyDown += ReportsView_PreviewKeyDown;
    }

    private void PreviewPrintButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowReportPreview();
    }

    private void ReportsView_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.P &&
            Keyboard.Modifiers == ModifierKeys.Control)
        {
            ShowReportPreview();

            e.Handled = true;
        }
    }

    private void ShowReportPreview()
    {
        if (DataContext is not ReportsViewModel viewModel)
        {
            return;
        }

        string clientName =
            viewModel.SelectedClientOption?.Name
            ?? "All Clients";

        var previewWindow =
            new ReportPreviewWindow(
                viewModel.Rows,
                viewModel.ReportPeriod,
                clientName,
                viewModel.GrandTotal);

        previewWindow.Owner =
            Window.GetWindow(this);

        previewWindow.ShowDialog();
    }
}