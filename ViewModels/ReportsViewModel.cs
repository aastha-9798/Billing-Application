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

    public ObservableCollection<ReportClientOption> ClientOptions { get; }
        = new();

    [ObservableProperty]
    private ReportClientOption? selectedClientOption;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private decimal grandTotal;

    [ObservableProperty]
    private string reportPeriod = string.Empty;

    private bool _loading;

    public ReportsViewModel()
    {
        _reportService = new ReportService();
        _clientService = new ClientService();

        DateTime today = DateTime.Today;

        StartDate = new DateTime(
            today.Year,
            today.Month,
            1);

        EndDate = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(
                today.Year,
                today.Month));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _loading = true;

        try
        {
            await LoadClientsAsync();

            SelectedClientOption =
                ClientOptions.FirstOrDefault();

            _loading = false;

            await LoadReportAsync();
        }
        catch
        {
            _loading = false;
            Rows.Clear();
            GrandTotal = 0;
            ReportPeriod = "Unable to load report.";
        }
    }

    private async Task LoadClientsAsync()
    {
        var clients =
            await _clientService.GetAllAsync();

        ClientOptions.Clear();

        ClientOptions.Add(
            new ReportClientOption
            {
                ClientId = null,
                Name = "All Clients"
            });

        foreach (var client in clients)
        {
            ClientOptions.Add(
                new ReportClientOption
                {
                    ClientId = client.Id,
                    Name = client.Name
                });
        }
    }

    partial void OnSelectedClientOptionChanged(
        ReportClientOption? value)
    {
        if (_loading)
        {
            return;
        }

        _ = LoadReportAsync();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (_loading)
        {
            return;
        }

        _ = LoadReportAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        if (_loading)
        {
            return;
        }

        _ = LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            if (EndDate < StartDate)
            {
                Rows.Clear();
                GrandTotal = 0;

                ReportPeriod =
                    "End date cannot be before start date.";

                return;
            }

            int? clientId =
                SelectedClientOption?.ClientId;

            var rows =
                await _reportService.GetReportAsync(
                    StartDate,
                    EndDate,
                    clientId);

            Rows.Clear();

            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            GrandTotal =
                Math.Round(
                    Rows.Sum(r => r.Amount),
                    2);

            ReportPeriod =
                $"{StartDate:dd MMM yyyy} - " +
                $"{EndDate:dd MMM yyyy}";
        }
        catch (Exception)
        {
            Rows.Clear();
            GrandTotal = 0;
            ReportPeriod = "Unable to load report.";
        }
    }
}