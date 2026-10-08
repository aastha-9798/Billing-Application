using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class ReportsViewModel : ObservableObject, IRefreshable
{
    private readonly ReportService _reportService = new();
    private readonly ClientService _clientService = new();

    // Filters are applied only after the client list has loaded.
    private bool _initialized;

    // Each load gets a number; results of older loads are ignored
    // so quick filter changes cannot show a stale report.
    private int _loadVersion;

    public ReportType[] ReportTypes { get; } = Enum.GetValues<ReportType>();

    public ObservableCollection<ReportClientOption> ClientOptions { get; } = new();


    // =========================================================
    // FILTERS
    // =========================================================

    [ObservableProperty]
    private ReportType selectedReportType = ReportType.Detailed;

    [ObservableProperty]
    private ReportClientOption? selectedClientOption;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;


    // =========================================================
    // RESULT
    // =========================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    private Report? currentReport;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public bool HasRows => CurrentReport?.Rows.Count > 0;


    public ReportsViewModel()
    {
        DateTime today = DateTime.Today;

        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);

        _ = InitializeAsync();
    }


    // =========================================================
    // INIT
    // =========================================================

    private async Task InitializeAsync()
    {
        await LoadClientOptionsAsync(selectClientId: null);
    }


    // =========================================================
    // REFRESH (returning to this screen)
    // =========================================================

    // Picks up new clients and new challans; the filters stay as they are.
    public async Task RefreshAsync()
    {
        await LoadClientOptionsAsync(SelectedClientOption?.ClientId);
    }

    private async Task LoadClientOptionsAsync(int? selectClientId)
    {
        // Rebuilding the list clears the selection; hold reloads meanwhile.
        _initialized = false;

        try
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

            SelectedClientOption =
                ClientOptions.FirstOrDefault(o => o.ClientId == selectClientId)
                ?? ClientOptions[0];
        }
        catch (Exception)
        {
            _initialized = true;
            StatusMessage = "Unable to load clients.";
            return;
        }

        _initialized = true;

        await LoadReportAsync();
    }


    // =========================================================
    // RELOAD ON FILTER CHANGE
    // =========================================================

    partial void OnSelectedReportTypeChanged(ReportType value) => Reload();

    partial void OnSelectedClientOptionChanged(ReportClientOption? value) => Reload();

    partial void OnStartDateChanged(DateTime value) => Reload();

    partial void OnEndDateChanged(DateTime value) => Reload();

    private void Reload()
    {
        if (_initialized)
        {
            _ = LoadReportAsync();
        }
    }


    // =========================================================
    // LOAD
    // =========================================================

    private async Task LoadReportAsync()
    {
        int version = ++_loadVersion;

        StatusMessage = string.Empty;

        if (SelectedClientOption == null)
        {
            CurrentReport = null;
            StatusMessage = "Select a client.";
            return;
        }

        if (EndDate < StartDate)
        {
            CurrentReport = null;
            StatusMessage = "End date cannot be before start date.";
            return;
        }

        try
        {
            var report = await _reportService.GenerateAsync(
                SelectedReportType,
                StartDate,
                EndDate,
                SelectedClientOption);

            if (version != _loadVersion)
            {
                return;
            }

            CurrentReport = report;

            if (report.Rows.Count == 0)
            {
                StatusMessage = "No challans found for this period.";
            }
        }
        catch (Exception)
        {
            if (version != _loadVersion)
            {
                return;
            }

            CurrentReport = null;
            StatusMessage = "Unable to load report.";
        }
    }
}
