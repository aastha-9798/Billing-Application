using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class RatesViewModel : ObservableObject, IRefreshable
{
    private readonly ClientService _clientService;
    private readonly PlateTypeService _plateTypeService;
    private readonly ClientPlateRateService _rateService;

    public ObservableCollection<Client> Clients { get; } = new();

    public ObservableCollection<PlateType> PlateTypes { get; } = new();

    public ObservableCollection<ClientPlateRate> Rates { get; } = new();

    [ObservableProperty]
    private Client? selectedClient;

    [ObservableProperty]
    private PlateType? selectedPlateType;

    [ObservableProperty]
    private ClientPlateRate? selectedRate;

    [ObservableProperty]
    private string newRate = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;


    public RatesViewModel()
    {
        _clientService = new ClientService();
        _plateTypeService = new PlateTypeService();
        _rateService = new ClientPlateRateService();

        _ = LoadDataAsync();
    }


    // =========================================================
    // LOAD DATA
    // =========================================================

    private async Task LoadDataAsync()
    {
        var clients = await _clientService.GetAllAsync();
        var plateTypes = await _plateTypeService.GetAllAsync();
        var rates = await _rateService.GetAllAsync();

        Clients.Clear();
        PlateTypes.Clear();
        Rates.Clear();

        foreach (var client in clients)
        {
            Clients.Add(client);
        }

        foreach (var plateType in plateTypes)
        {
            PlateTypes.Add(plateType);
        }

        foreach (var rate in rates)
        {
            Rates.Add(rate);
        }
    }


    // =========================================================
    // REFRESH (returning to this screen)
    // =========================================================

    // Picks up clients and plate types changed on other screens.
    // Reloading the lists clears the selections, so the form is
    // remembered first and put back afterwards.
    public async Task RefreshAsync()
    {
        int? rateId = SelectedRate?.Id;
        int? clientId = SelectedClient?.Id;
        int? plateTypeId = SelectedPlateType?.Id;
        string rate = NewRate;
        string status = StatusMessage;

        try
        {
            await LoadDataAsync();
        }
        catch (Exception)
        {
            StatusMessage = "Unable to load rates.";
            return;
        }

        SelectedRate = Rates.FirstOrDefault(r => r.Id == rateId);
        SelectedClient = Clients.FirstOrDefault(c => c.Id == clientId);
        SelectedPlateType = PlateTypes.FirstOrDefault(p => p.Id == plateTypeId);
        NewRate = rate;
        StatusMessage = status;
    }


    // =========================================================
    // ADD RATE
    // =========================================================

    [RelayCommand]
    private void AddRate()
    {
        SelectedRate = null;

        SelectedClient = null;
        SelectedPlateType = null;
        NewRate = string.Empty;

        StatusMessage = string.Empty;
    }


    // =========================================================
    // SELECT EXISTING RATE
    // =========================================================

    partial void OnSelectedRateChanged(ClientPlateRate? value)
    {
        if (value == null)
        {
            return;
        }

        SelectedClient = value.Client;
        SelectedPlateType = value.PlateType;
        NewRate = value.Rate.ToString();

        StatusMessage = string.Empty;
    }


    // =========================================================
    // SAVE / UPDATE RATE
    // =========================================================

    [RelayCommand]
    private async Task SaveRateAsync()
    {
        StatusMessage = string.Empty;

        // -----------------------------------------------------
        // Validation
        // -----------------------------------------------------

        if (SelectedClient == null)
        {
            StatusMessage = "Select a client.";
            return;
        }

        if (SelectedPlateType == null)
        {
            StatusMessage = "Select a plate type.";
            return;
        }

        if (!decimal.TryParse(NewRate, out decimal rate))
        {
            StatusMessage = "Enter a valid rate.";
            return;
        }

        if (rate < 0)
        {
            StatusMessage = "Rate cannot be negative.";
            return;
        }


        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        try
        {
            if (SelectedRate == null)
            {
                // ---------------------------------------------
                // ADD NEW RATE
                // ---------------------------------------------

                var newRateRecord =
                    await _rateService.CreateAsync(
                        SelectedClient.Id,
                        SelectedPlateType.Id,
                        rate);

                Rates.Add(newRateRecord);

                StatusMessage = "Rate added successfully.";
            }
            else
            {
                // ---------------------------------------------
                // UPDATE EXISTING RATE
                // ---------------------------------------------

                var updatedRate =
                    await _rateService.UpdateAsync(
                        SelectedRate.Id,
                        SelectedClient.Id,
                        SelectedPlateType.Id,
                        rate);

                int index = Rates.IndexOf(SelectedRate);

                if (index >= 0)
                {
                    Rates[index] = updatedRate;
                }

                StatusMessage = "Rate updated successfully.";
            }


            // -------------------------------------------------
            // PREPARE FOR NEXT ENTRY
            //
            // IMPORTANT:
            // Client is intentionally preserved.
            // -------------------------------------------------

            SelectedRate = null;

            SelectedPlateType = null;

            NewRate = string.Empty;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Unable to save the rate.";
        }
    }


    // =========================================================
    // DELETE RATE
    // =========================================================

    [RelayCommand]
    private async Task DeleteRateAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedRate == null)
        {
            StatusMessage = "Select a rate first.";
            return;
        }

        try
        {
            await _rateService.DeleteAsync(SelectedRate.Id);

            Rates.Remove(SelectedRate);

            SelectedRate = null;

            SelectedClient = null;

            SelectedPlateType = null;

            NewRate = string.Empty;

            StatusMessage = "Rate deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Unable to delete the rate.";
        }
    }
}