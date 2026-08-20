using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;
using PlateBilling.Services;

namespace PlateBilling.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly ClientService _clientService;

    public ObservableCollection<Client> Clients { get; } = new();

    [ObservableProperty]
    private Client? selectedClient;

    [ObservableProperty]
    private string newClientName = string.Empty;

    [ObservableProperty]
    private string newClientGSTIN = string.Empty;

    [ObservableProperty]
    private string newClientPhone = string.Empty;

    [ObservableProperty]
    private string newClientAddress = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public ClientsViewModel()
    {
        _clientService = new ClientService();

        _ = LoadClientsAsync();
    }

    private async Task LoadClientsAsync() //loads clients from the database and populates the Clients collection
    {
        var clients = await _clientService.GetAllAsync();

        Clients.Clear();

        foreach (var client in clients)
        {
            Clients.Add(client);
        }
    }

    [RelayCommand]
    private void AddClient() //clears the input fields and prepares the form for adding a new client
    {
        SelectedClient = null;

        NewClientName = string.Empty;
        NewClientGSTIN = string.Empty;
        NewClientPhone = string.Empty;
        NewClientAddress = string.Empty;
        StatusMessage = string.Empty;
    }

    partial void OnSelectedClientChanged(Client? value)
        //when a client is selected from the list, populates the input fields with the client's details
    {
        if (value == null)
            return;

        NewClientName = value.Name;
        NewClientGSTIN = value.GSTIN;
        NewClientPhone = value.Phone ?? string.Empty;
        NewClientAddress = value.Address;

        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveClientAsync()
    {
        StatusMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewClientName))
        {
            StatusMessage = "Client name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewClientGSTIN))
        {
            StatusMessage = "GSTIN is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewClientAddress))
        {
            StatusMessage = "Address is required.";
            return;
        }

        try
        {
            if (SelectedClient == null)
            {
                var client = await _clientService.CreateAsync(
                    NewClientName,
                    NewClientGSTIN,
                    NewClientPhone,
                    NewClientAddress);

                Clients.Add(client);

                StatusMessage = "Client added successfully.";
            }
            else
            {
                var updatedClient = await _clientService.UpdateAsync(
                                            SelectedClient.Id,
                                            NewClientName,
                                            NewClientGSTIN,
                                            NewClientPhone,
                                            NewClientAddress);

                int index = Clients.IndexOf(SelectedClient);

                if (index >= 0)
                {
                    Clients[index] = updatedClient;
                }

                StatusMessage = "Client updated successfully.";
            }

            SelectedClient = null;

            NewClientName = string.Empty;
            NewClientGSTIN = string.Empty;
            NewClientPhone = string.Empty;
            NewClientAddress = string.Empty;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Unable to save the client.";
        }
    }

    [RelayCommand]
    private async Task DeleteClientAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedClient == null)
        {
            StatusMessage = "Select a client first.";
            return;
        }

        try
        {
            await _clientService.DeleteAsync(SelectedClient.Id);

            Clients.Remove(SelectedClient);

            SelectedClient = null;

            NewClientName = string.Empty;
            NewClientGSTIN = string.Empty;
            NewClientPhone = string.Empty;
            NewClientAddress = string.Empty;

            StatusMessage = "Client deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (DbUpdateException)
        {
            StatusMessage =
                "This client cannot be deleted because it is being used by existing records.";
        }
    }
}