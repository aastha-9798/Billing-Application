using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reportService;
    private readonly ClientService _clientService;

    public ObservableCollection<ReportRow> Rows { get; } = new();

    public ObservableCollection<CumulativeReportRow> CumulativeRows { get; } = new();

    public ObservableCollection<ReportClientOption> ClientOptions { get; } = new();

    // Report type options for the ComboBox
    public ReportType[] ReportTypes { get; } =
        [ReportType.Detailed, ReportType.Cumulative];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailedReport))]
    [NotifyPropertyChangedFor(nameof(IsCumulativeReport))]
    private ReportType selectedReportType = ReportType.Detailed;

    public bool IsDetailedReport => SelectedReportType == ReportType.Detailed;
    public bool IsCumulativeReport => SelectedReportType == ReportType.Cumulative;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSingleClient))]
    [NotifyPropertyChangedFor(nameof(ReportClientInfo))]
    private ReportClientOption? selectedClientOption;

    // True when a specific client (not "All Clients") is selected
    public bool IsSingleClient =>
        SelectedClientOption?.ClientId != null;

    // Header line shown for single-client detailed report
    public string ReportClientInfo
    {
        get
        {
            if (SelectedClientOption?.ClientId == null)
                return string.Empty;

            string info = SelectedClientOption.Name;

            if (!string.IsNullOrWhiteSpace(SelectedClientOption.Phone))
                info += $"  |  {SelectedClientOption.Phone}";

            return info;
        }
    }

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private decimal grandTotal;

    [ObservableProperty]
    private string reportPeriod = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    private bool _loading;

    public ReportsViewModel()
    {
        _reportService = new ReportService();
        _clientService = new ClientService();

        DateTime today = DateTime.Today;

        StartDate = new DateTime(today.Year, today.Month, 1);

        EndDate = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(today.Year, today.Month));

        _ = InitializeAsync();
    }


    // =========================================================
    // INIT
    // =========================================================

    private async Task InitializeAsync()
    {
        _loading = true;

        try
        {
            await LoadClientsAsync();

            SelectedClientOption = ClientOptions.FirstOrDefault();

            _loading = false;

            await LoadReportAsync();
        }
        catch
        {
            _loading = false;
            Rows.Clear();
            CumulativeRows.Clear();
            GrandTotal = 0;
            ReportPeriod = "Unable to load report.";
        }
    }

    private async Task LoadClientsAsync()
    {
        var clients = await _clientService.GetAllAsync();

        ClientOptions.Clear();

        ClientOptions.Add(new ReportClientOption
        {
            ClientId = null,
            Name = "All Clients"
        });

        foreach (var client in clients)
        {
            ClientOptions.Add(new ReportClientOption
            {
                ClientId = client.Id,
                Name = client.Name,
                Phone = client.Phone
            });
        }
    }


    // =========================================================
    // PROPERTY CHANGE HANDLERS
    // =========================================================

    partial void OnSelectedReportTypeChanged(ReportType value)
    {
        if (_loading) return;
        _ = LoadReportAsync();
    }

    partial void OnSelectedClientOptionChanged(ReportClientOption? value)
    {
        if (_loading) return;
        _ = LoadReportAsync();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (_loading) return;
        _ = LoadReportAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        if (_loading) return;
        _ = LoadReportAsync();
    }


    // =========================================================
    // LOAD REPORT
    // =========================================================

    private async Task LoadReportAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            if (EndDate < StartDate)
            {
                Rows.Clear();
                CumulativeRows.Clear();
                GrandTotal = 0;
                ReportPeriod = "End date cannot be before start date.";
                return;
            }

            ReportPeriod =
                $"{StartDate:dd MMM yyyy}  –  {EndDate:dd MMM yyyy}";

            if (SelectedReportType == ReportType.Detailed)
            {
                await LoadDetailedAsync();
            }
            else
            {
                await LoadCumulativeAsync();
            }
        }
        catch (Exception)
        {
            Rows.Clear();
            CumulativeRows.Clear();
            GrandTotal = 0;
            ReportPeriod = "Unable to load report.";
        }
    }

    private async Task LoadDetailedAsync()
    {
        int? clientId = SelectedClientOption?.ClientId;

        var rows = await _reportService.GetReportAsync(
            StartDate, EndDate, clientId);

        Rows.Clear();

        foreach (var row in rows)
            Rows.Add(row);

        GrandTotal = Math.Round(Rows.Sum(r => r.Amount), 2);
    }

    private async Task LoadCumulativeAsync()
    {
        // Cumulative requires a specific client
        if (SelectedClientOption?.ClientId == null)
        {
            CumulativeRows.Clear();
            GrandTotal = 0;
            ErrorMessage =
                "Select a specific client for the cumulative report.";
            return;
        }

        var rows = await _reportService.GetCumulativeReportAsync(
            StartDate,
            EndDate,
            SelectedClientOption.ClientId.Value);

        CumulativeRows.Clear();

        foreach (var row in rows)
            CumulativeRows.Add(row);

        GrandTotal = Math.Round(CumulativeRows.Sum(r => r.Amount), 2);
    }
}
