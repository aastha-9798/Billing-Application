using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using PlateBilling.Models;
using PlateBilling.Services;
using PlateBilling.ViewModels;

namespace PlateBilling.Views;

public partial class ReportsView : UserControl
{
    private readonly ReportsViewModel _viewModel;

    public ReportsView()
    {
        // Created before InitializeComponent: the Save / Print buttons
        // query Export_CanExecute while the XAML is being loaded.
        _viewModel = new ReportsViewModel();

        InitializeComponent();

        DataContext = _viewModel;

        // Enable / disable Save PDF and Print as soon as a report loads.
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReportsViewModel.HasRows))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        };

        // Focus inside the view so Ctrl+S / Ctrl+P work straight away.
        Loaded += (_, _) => Dispatcher.InvokeAsync(
            () => ReportTypeInput.Focus(),
            DispatcherPriority.Input);
    }


    // =========================================================
    // SAVE PDF / SAVE EXCEL / PRINT
    // =========================================================

    // Ctrl+E saves as Excel (Save / Print use the standard commands).
    public static readonly RoutedUICommand SaveExcelCommand = new(
        "Save Excel",
        nameof(SaveExcelCommand),
        typeof(ReportsView),
        [new KeyGesture(Key.E, ModifierKeys.Control)]);

    private void Export_CanExecute(
        object sender,
        CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _viewModel.HasRows;
    }

    private void SavePdf_Executed(
        object sender,
        ExecutedRoutedEventArgs e)
    {
        SaveReport("PDF", "PDF files (*.pdf)|*.pdf", ".pdf", ReportExporter.SavePdf);
    }

    private void SaveExcel_Executed(
        object sender,
        ExecutedRoutedEventArgs e)
    {
        SaveReport("Excel", "Excel workbook (*.xlsx)|*.xlsx", ".xlsx", ReportExporter.SaveExcel);
    }

    private void SaveReport(
        string formatName,
        string filter,
        string extension,
        Action<Report, string> save)
    {
        var report = _viewModel.CurrentReport;

        if (report == null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = $"Save Report as {formatName}",
            Filter = filter,
            DefaultExt = extension,
            AddExtension = true,
            FileName = ReportExporter.SuggestFileName(report)
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            save(report, dialog.FileName);

            MessageBox.Show(
                $"Report saved as {formatName}.",
                "Report Saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // e.g. the file is open in Excel.
            MessageBox.Show(
                $"Unable to save the report.\n\n{ex.Message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Print_Executed(
        object sender,
        ExecutedRoutedEventArgs e)
    {
        var report = _viewModel.CurrentReport;

        if (report == null)
        {
            return;
        }

        var dialog = new PrintDialog();

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            ReportExporter.Print(report, dialog);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to print the report.\n\n{ex.Message}",
                "Print Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
