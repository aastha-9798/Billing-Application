using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.ViewModels;

public partial class BillGenerationViewModel : ObservableObject
{
    public ObservableCollection<Client> Clients { get; } = new();

    public ObservableCollection<Challan> BillLines { get; } = new();


    // =========================================================
    // FILTER FIELDS
    // =========================================================

    [ObservableProperty]
    private Client? selectedClient;

    [ObservableProperty]
    private DateTime fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime toDate = DateTime.Today;


    // =========================================================
    // SUMMARY
    // =========================================================

    [ObservableProperty]
    private decimal subtotal;

    [ObservableProperty]
    private decimal gstRate = 18m;

    [ObservableProperty]
    private decimal gstAmount;

    [ObservableProperty]
    private decimal grandTotal;

    [ObservableProperty]
    private bool hasResults;

    [ObservableProperty]
    private string statusMessage = string.Empty;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public BillGenerationViewModel()
    {
        _ = LoadClientsAsync();
    }


    // =========================================================
    // LOAD CLIENTS
    // =========================================================

    private async Task LoadClientsAsync()
    {
        try
        {
            using var db = new AppDbContext();

            var clients = await db.Clients
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            Clients.Clear();

            foreach (var client in clients)
            {
                Clients.Add(client);
            }
        }
        catch (Exception)
        {
            StatusMessage = "Unable to load clients.";
        }
    }


    // =========================================================
    // GENERATE BILL
    // =========================================================

    [RelayCommand]
    private async Task GenerateBillAsync()
    {
        StatusMessage = string.Empty;
        HasResults = false;

        if (SelectedClient == null)
        {
            StatusMessage = "Select a client.";
            return;
        }

        if (FromDate > ToDate)
        {
            StatusMessage = "From date cannot be after To date.";
            return;
        }

        try
        {
            using var db = new AppDbContext();

            // Inclusive date range — compare date-only part
            DateTime start = FromDate.Date;
            DateTime end = ToDate.Date.AddDays(1);

            var challans = await db.Challans
                .AsNoTracking()
                .Include(c => c.Client)
                .Include(c => c.PlateType)
                .Where(c =>
                    c.ClientId == SelectedClient.Id &&
                    c.Date >= start &&
                    c.Date < end)
                .OrderBy(c => c.Date)
                .ThenBy(c => c.ChallanNo)
                .ToListAsync();

            BillLines.Clear();

            foreach (var challan in challans)
            {
                BillLines.Add(challan);
            }

            if (challans.Count == 0)
            {
                StatusMessage = "No challans found for the selected period.";
                return;
            }

            Subtotal = challans.Sum(c => c.TotalCost);
            GstAmount = Math.Round(Subtotal * GstRate / 100, 2);
            GrandTotal = Subtotal + GstAmount;

            HasResults = true;
        }
        catch (Exception)
        {
            StatusMessage = "Unable to generate the bill.";
        }
    }


    // =========================================================
    // RECALCULATE WHEN GST RATE CHANGES
    // =========================================================

    partial void OnGstRateChanged(decimal value)
    {
        if (!HasResults) return;

        GstAmount = Math.Round(Subtotal * value / 100, 2);
        GrandTotal = Subtotal + GstAmount;
    }
}
