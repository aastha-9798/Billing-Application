using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class PlateTypesViewModel : ObservableObject
{
    private readonly PlateTypeService _plateTypeService;

    public ObservableCollection<PlateType> PlateTypes { get; } = new();

    [ObservableProperty]
    private PlateType? selectedPlateType;

    [ObservableProperty]
    private string newPlateCode = string.Empty;

    [ObservableProperty]
    private string newPlateLength = string.Empty;

    [ObservableProperty]
    private string newPlateBreadth = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public PlateTypesViewModel()
    {
        _plateTypeService = new PlateTypeService();

        _ = LoadPlateTypesAsync();
    }

    private async Task LoadPlateTypesAsync()
    {
        var plateTypes = await _plateTypeService.GetAllAsync();

        PlateTypes.Clear();

        foreach (var plateType in plateTypes)
        {
            PlateTypes.Add(plateType);
        }
    }

    [RelayCommand]
    private void AddPlateType()
    {
        SelectedPlateType = null;

        NewPlateCode = string.Empty;
        NewPlateLength = string.Empty;
        NewPlateBreadth = string.Empty;

        StatusMessage = string.Empty;
    }

    partial void OnSelectedPlateTypeChanged(PlateType? value)
    {
        if (value == null)
            return;

        NewPlateCode = value.Code;
        NewPlateLength = value.Length.ToString();
        NewPlateBreadth = value.Breadth.ToString();

        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SavePlateTypeAsync()
    {
        StatusMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewPlateCode))
        {
            StatusMessage = "Plate type code is required.";
            return;
        }

        if (!decimal.TryParse(NewPlateLength, out decimal length))
        {
            StatusMessage = "Enter a valid length.";
            return;
        }

        if (!decimal.TryParse(NewPlateBreadth, out decimal breadth))
        {
            StatusMessage = "Enter a valid breadth.";
            return;
        }

        if (length <= 0 || breadth <= 0)
        {
            StatusMessage = "Length and breadth must be greater than zero.";
            return;
        }

        try
        {
            if (SelectedPlateType == null)
            {
                var plateType = await _plateTypeService.CreateAsync(
                    NewPlateCode,
                    length,
                    breadth);

                PlateTypes.Add(plateType);

                StatusMessage = "Plate type added successfully.";
            }
            else
            {
                var updatedPlateType =
                    await _plateTypeService.UpdateAsync(
                        SelectedPlateType.Id,
                        NewPlateCode,
                        length,
                        breadth);

                int index = PlateTypes.IndexOf(SelectedPlateType);

                if (index >= 0)
                {
                    PlateTypes[index] = updatedPlateType;
                }

                StatusMessage = "Plate type updated successfully.";
            }

            SelectedPlateType = null;

            NewPlateCode = string.Empty;
            NewPlateLength = string.Empty;
            NewPlateBreadth = string.Empty;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Unable to save the plate type.";
        }
    }

    [RelayCommand]
    private async Task DeletePlateTypeAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedPlateType == null)
        {
            StatusMessage = "Select a plate type first.";
            return;
        }

        try
        {
            await _plateTypeService.DeleteAsync(
                SelectedPlateType.Id);

            PlateTypes.Remove(SelectedPlateType);

            SelectedPlateType = null;

            NewPlateCode = string.Empty;
            NewPlateLength = string.Empty;
            NewPlateBreadth = string.Empty;

            StatusMessage = "Plate type deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage =
                "This plate type cannot be deleted because it is being used by existing records.";
        }
    }
}
