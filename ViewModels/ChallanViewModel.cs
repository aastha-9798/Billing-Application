using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class ChallanViewModel : ObservableObject
{
    private readonly ClientService _clientService;
    private readonly PlateTypeService _plateTypeService;
    private readonly ClientPlateRateService _rateService;
    private readonly ApplicationSettingsService _settingsService;
    private readonly ChallanCalculator _challanCalculator;
    private readonly ChallanService _challanService;

    public ObservableCollection<Client> Clients { get; } = new();

    public ObservableCollection<PlateType> PlateTypes { get; } = new();

    public ObservableCollection<Challan> Challans { get; } = new();


    // =========================================================
    // FORM FIELDS
    // =========================================================

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private Client? selectedClient;

    [ObservableProperty]
    private int? challanNo;

    [ObservableProperty]
    private string plateDescription = string.Empty;

    [ObservableProperty]
    private PlateType? selectedPlateType;

    [ObservableProperty]
    private int? quantity;

    [ObservableProperty]
    private bool areaBilling;

    [ObservableProperty]
    private decimal totalCost;

    [ObservableProperty]
    private decimal? currentClientRate;


    // =========================================================
    // STATUS
    // =========================================================

    [ObservableProperty]
    private string statusMessage = string.Empty;


    // =========================================================
    // EDITING STATE
    // =========================================================

    [ObservableProperty]
    private Challan? selectedChallan;

    [ObservableProperty]
    private bool isEditing;

    // Suppresses UpdateCalculationAsync while loading a challan into the form.
    private bool _loadingChallan;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ChallanViewModel() //objects of different services. 
    {
        _clientService = new ClientService();
        _plateTypeService = new PlateTypeService();
        _rateService = new ClientPlateRateService();
        _settingsService = new ApplicationSettingsService();
        _challanCalculator = new ChallanCalculator();
        _challanService = new ChallanService();

        _ = LoadMasterDataAsync();
        _ = LoadChallansAsync();
    }


    // =========================================================
    // LOAD MASTER DATA
    // =========================================================

    private async Task LoadMasterDataAsync()
    {
        try
        {
            var clients = await _clientService.GetAllAsync();
            var plateTypes = await _plateTypeService.GetAllAsync();

            Clients.Clear();
            PlateTypes.Clear();

            foreach (var client in clients)
            {
                Clients.Add(client);
            }

            foreach (var plateType in plateTypes)
            {
                PlateTypes.Add(plateType);
            }
        }
        catch (Exception)
        {
            StatusMessage = "Unable to load billing data.";
        }
    }


    // =========================================================
    // LOAD Challans
    // =========================================================

    private async Task LoadChallansAsync()
    {
        try
        {
            var challans = await _challanService.GetAllAsync(); //sorting : challan no -> time

            Challans.Clear();

            foreach (var challan in challans)
            {
                Challans.Add(challan);
            }
        }
        catch (Exception)
        {
            StatusMessage = "Unable to load challan records.";
        }
    }


    // =========================================================
    // CHALLAN SELECTION / EDIT
    // =========================================================

    partial void OnSelectedChallanChanged(Challan? value)
    {
        if (value == null)
        {
            return;
        }

        // -----------------------------------------
        // Load selected challan into the form.
        // Suppress recalculation while setting fields
        // so the stored TotalCost is preserved.
        // -----------------------------------------
        _loadingChallan = true;

        IsEditing = true;

        Date = value.Date;
        SelectedClient = value.Client;
        ChallanNo = value.ChallanNo;
        PlateDescription = value.PlateDescription;
        SelectedPlateType = value.PlateType;
        Quantity = value.Quantity;
        AreaBilling = value.AreaBilling;
        TotalCost = value.TotalCost;
        CurrentClientRate = value.AreaBilling ? null : value.AppliedRate;

        _loadingChallan = false;

        StatusMessage = "Editing challan entry.";
    }


    // =========================================================
    // CLIENT CHANGE
    // =========================================================

    partial void OnSelectedClientChanged(Client? value)
    {
        if (_loadingChallan) return;
        _ = UpdateCalculationAsync();
    }


    // =========================================================
    // PLATE TYPE CHANGE
    // =========================================================

    partial void OnSelectedPlateTypeChanged(PlateType? value)
    {
        if (_loadingChallan) return;
        _ = UpdateCalculationAsync();
    }


    // =========================================================
    // QUANTITY CHANGE
    // =========================================================

    partial void OnQuantityChanged(int? value)
    {
        if (_loadingChallan) return;
        _ = UpdateCalculationAsync();
    }


    // =========================================================
    // AREA BILLING CHANGE
    // =========================================================

    partial void OnAreaBillingChanged(bool value)
    {
        if (_loadingChallan) return;
        _ = UpdateCalculationAsync();
    }


    // =========================================================
    // CALCULATE TOTAL
    // =========================================================

    private async Task UpdateCalculationAsync()
    {
        TotalCost = 0;
        CurrentClientRate = null;

        if (SelectedClient == null ||
            SelectedPlateType == null ||
            Quantity is null ||
            Quantity <= 0)
        {
            return;
        }

        try
        {
            decimal globalAreaRate =
                await _settingsService.GetGlobalAreaRateAsync();

            var clientRate =
                await _rateService.GetRateAsync(
                    SelectedClient.Id,
                    SelectedPlateType.Id);

            if (!AreaBilling)
            {
                if (clientRate == null)
                {
                    StatusMessage =
                        "No rate is configured for this client and plate type.";

                    return;
                }

                CurrentClientRate = clientRate.Rate;
            }

            TotalCost = Math.Round(
                _challanCalculator.CalculateTotal(
                    AreaBilling,
                    clientRate,
                    SelectedPlateType,
                    Quantity.Value,
                    globalAreaRate),
                2);

            // Don't remove the edit status while editing.
            if (!IsEditing)
            {
                StatusMessage = string.Empty;
            }
        }
        catch (Exception)
        {
            TotalCost = 0;
            StatusMessage = "Unable to calculate the total.";
        }
    }


    // =========================================================
    // SAVE / UPDATE
    // =========================================================

    [RelayCommand]
    private async Task SaveChallanAsync()
    {
        StatusMessage = string.Empty;

        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (SelectedClient == null)
        {
            StatusMessage = "Select a client.";
            return;
        }

        if (ChallanNo is null || ChallanNo <= 0)
        {
            StatusMessage = "Challan number must be greater than zero.";
            return;
        }

        if (SelectedPlateType == null)
        {
            StatusMessage = "Select a plate type.";
            return;
        }

        if (Quantity is null || Quantity <= 0)
        {
            StatusMessage = "Quantity must be greater than zero.";
            return;
        }

        if (TotalCost <= 0)
        {
            StatusMessage = "A valid total cost is required.";
            return;
        }


        try
        {
            decimal globalAreaRate =
                await _settingsService.GetGlobalAreaRateAsync();

            var clientRate =
                await _rateService.GetRateAsync(
                    SelectedClient.Id,
                    SelectedPlateType.Id);

            if (!AreaBilling && clientRate == null)
            {
                StatusMessage =
                    "No rate is configured for this client and plate type.";

                return;
            }


            decimal appliedRate =
                AreaBilling
                    ? 0m
                    : clientRate!.Rate; 
            // Safe to use ! because we already checked for null above.
            // ! means we are telling the compiler that we are sure this value is not null, so it won't give a warning about possible null reference.


            // =================================================
            // UPDATE EXISTING ENTRY
            // =================================================

            if (IsEditing && SelectedChallan != null)
            {
                var updatedChallan = await _challanService.UpdateAsync(
                    SelectedChallan.Id,
                    Date,
                    SelectedClient.Id,
                    ChallanNo.Value,
                    PlateDescription,
                    SelectedPlateType.Id,
                    Quantity.Value,
                    AreaBilling,
                    appliedRate,
                    SelectedPlateType.Length,
                    SelectedPlateType.Breadth,
                    globalAreaRate,
                    TotalCost);


                // ---------------------------------------------
                // Update the existing row in the collection
                // ---------------------------------------------

                int index = Challans.IndexOf(SelectedChallan);

                if (index >= 0)
                {
                    Challans[index] = updatedChallan;
                }


                // ---------------------------------------------
                // Exit edit mode
                // ---------------------------------------------

                SelectedChallan = null;
                IsEditing = false;


                // ---------------------------------------------
                // Prepare for next entry
                // ---------------------------------------------
                Date = updatedChallan.Date;
                SelectedClient = updatedChallan.Client;
                ChallanNo = updatedChallan.ChallanNo +1 ;
                PlateDescription = string.Empty;
                SelectedPlateType = updatedChallan.PlateType;
                Quantity = null;
                AreaBilling = false;
                TotalCost = 0;
                CurrentClientRate = null;

                StatusMessage =
                    "Challan entry updated successfully.";

                return;
            }


            // =================================================
            // CREATE NEW ENTRY
            // =================================================

            var challan = await _challanService.CreateAsync(
                Date,
                SelectedClient.Id,
                ChallanNo.Value,
                PlateDescription,
                SelectedPlateType.Id,
                Quantity.Value,
                AreaBilling,
                appliedRate,
                SelectedPlateType.Length,
                SelectedPlateType.Breadth,
                globalAreaRate,
                TotalCost);


            Challans.Add(challan);


            // ---------------------------------------------
            // Reset fields for the next entry.
            //
            // Keep:
            //   Date
            //   Client
            //   Challan No.
            //
            // Clear:
            //   Plate Description
            //   Plate Type
            //   Quantity
            //   Area Billing
            //   Amount
            //   Client Rate
            // ---------------------------------------------
            Date = challan.Date;
            SelectedClient= challan.Client;
            SelectedPlateType= challan.PlateType;
            ChallanNo = challan.ChallanNo+1;
            PlateDescription = string.Empty;
            SelectedPlateType = null;
            Quantity = null;
            AreaBilling = false;
            TotalCost = 0;
            CurrentClientRate = null;

            StatusMessage =
                "Challan entry saved successfully.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception)
        {
            StatusMessage =
                IsEditing
                    ? "Unable to update the challan entry."
                    : "Unable to save the challan entry.";
        }
    }


    // =========================================================
    // DELETE CHALLAN
    // =========================================================

    [RelayCommand]
    private async Task DeleteChallanAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedChallan == null)
        {
            StatusMessage = "Select a challan entry first.";
            return;
        }

        try
        {
            int challanId = SelectedChallan.Id;

            await _challanService.DeleteAsync(challanId);

            Challans.Remove(SelectedChallan);

            SelectedChallan = null;
            IsEditing = false;

            // ---------------------------------------------
            // Clear editable fields
            //
            // Keep:
            //   Date
            //   Client
            //   Challan No.
            // ---------------------------------------------

            PlateDescription = string.Empty;
            SelectedPlateType = null;
            Quantity = null;
            AreaBilling = false;
            TotalCost = 0;
            CurrentClientRate = null;

            StatusMessage =
                "Challan entry deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception)
        {
            StatusMessage =
                "Unable to delete the challan entry.";
        }
    }


    // =========================================================
    // CANCEL EDIT
    // =========================================================

    [RelayCommand]
    private void CancelEdit()
    {
        SelectedChallan = null;

        IsEditing = false;

        // ---------------------------------------------
        // Clear fields that belong to the selected
        // transaction.
        //
        // Keep:
        //   Date
        //   Client
        //   Challan No.
        // ---------------------------------------------

        PlateDescription = string.Empty;
        SelectedPlateType = null;
        Quantity = null;
        AreaBilling = false;
        TotalCost = 0;
        CurrentClientRate = null;

        StatusMessage = string.Empty;
    }
}