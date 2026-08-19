using Microsoft.EntityFrameworkCore;
using PlateBilling.Models;

namespace PlateBilling.Data;

public class AppDbContext : DbContext
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<PlateType> PlateTypes { get; set; }
    public DbSet<ClientPlateRate> ClientPlateRates { get; set; }
    public DbSet<Challan> Challans { get; set; }
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=platebilling.db");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<PlateType>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<ClientPlateRate>()
            .HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClientPlateRate>()
            .HasOne(r => r.PlateType)
            .WithMany()
            .HasForeignKey(r => r.PlateTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClientPlateRate>()
            .HasIndex(r => new { r.ClientId, r.PlateTypeId })
            .IsUnique();

        modelBuilder.Entity<Challan>()
            .HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Challan>()
            .HasOne(c => c.PlateType)
            .WithMany()
            .HasForeignKey(c => c.PlateTypeId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}