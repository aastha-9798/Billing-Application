using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ClientService
{
    public async Task<List<Client>> GetAllAsync()
    {
        using var db = new AppDbContext();

        return await db.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Client> CreateAsync(
        string name,
        string gstin,
        string? phone,
        string address)
    {
        using var db = new AppDbContext();

        name = name.Trim();
        gstin = gstin.Trim();

        bool exists = await db.Clients
            .AnyAsync(c => c.Name == name);

        if (exists)
        {
            throw new InvalidOperationException(
                "A client with this name already exists.");
        }

        var client = new Client
        {
            Name = name,
            GSTIN = gstin,
            Phone = string.IsNullOrWhiteSpace(phone)
                ? null
                : phone.Trim(),
            Address= address.Trim()
        };

        db.Clients.Add(client);

        await db.SaveChangesAsync();

        return client;
    }

    public async Task<Client> UpdateAsync(
       int id,
       string name,
       string gstin,
       string? phone,
       string address)
    {
        using var db = new AppDbContext();

        name = name.Trim();
        gstin = gstin.Trim();

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            throw new InvalidOperationException(
                "Client was not found.");
        }

        bool duplicateName = await db.Clients
            .AnyAsync(c => c.Id != id && c.Name == name);

        if (duplicateName)
        {
            throw new InvalidOperationException(
                "A client with this name already exists.");
        }

        client.Name = name;
        client.GSTIN = gstin;
        client.Phone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();
        client.Address = address.Trim();

        await db.SaveChangesAsync();
        return client;
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new AppDbContext();

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            throw new InvalidOperationException(
                "Client was not found.");
        }

        db.Clients.Remove(client);

        await db.SaveChangesAsync();
    }
}