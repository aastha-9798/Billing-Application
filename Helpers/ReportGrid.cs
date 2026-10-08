using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PlateBilling.Models;

namespace PlateBilling.Helpers;

/// <summary>
/// Attached property that fills a DataGrid from a <see cref="Report"/>:
/// one column per <see cref="ReportColumn"/>, numbers right-aligned.
///
/// Usage: helpers:ReportGrid.Report="{Binding CurrentReport}"
/// </summary>
public static class ReportGrid
{
    public static readonly DependencyProperty ReportProperty =
        DependencyProperty.RegisterAttached(
            "Report",
            typeof(Report),
            typeof(ReportGrid),
            new PropertyMetadata(null, OnReportChanged));

    public static Report? GetReport(DataGrid grid) =>
        (Report?)grid.GetValue(ReportProperty);

    public static void SetReport(DataGrid grid, Report? value) =>
        grid.SetValue(ReportProperty, value);

    private static void OnReportChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        var report = e.NewValue as Report;

        grid.Columns.Clear();
        grid.ItemsSource = report?.Rows;

        if (report == null)
        {
            return;
        }

        foreach (var column in report.Columns)
        {
            grid.Columns.Add(CreateColumn(column));
        }
    }

    private static DataGridTextColumn CreateColumn(ReportColumn column)
    {
        var alignment = column.IsNumeric
            ? TextAlignment.Right
            : TextAlignment.Left;

        var cellStyle = new Style(typeof(TextBlock));
        cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
        cellStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        cellStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(12, 0, 12, 0)));

        return new DataGridTextColumn
        {
            // Header text is a TextBlock so it can be aligned with
            // its column (the header style stretches its content).
            Header = new TextBlock
            {
                Text = column.Header,
                TextAlignment = alignment
            },
            Binding = new Binding(column.Path)
            {
                StringFormat = column.Format == null ? null : $"{{0:{column.Format}}}"
            },
            Width = new DataGridLength(column.Width, DataGridLengthUnitType.Star),
            ElementStyle = cellStyle
        };
    }
}
