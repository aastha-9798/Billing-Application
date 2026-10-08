using Microsoft.EntityFrameworkCore;
using PlateBilling.Data;
using PlateBilling.Models;

namespace PlateBilling.Services;

public class ReportService
{
    // Report heading when the report covers all clients.
    private const string BusinessName = "BIITs Computers";

    // A single challan line, priced as it was saved.
    private sealed record Line(
        string ClientName,
        int ChallanNo,
        string PlateTypeCode,
        bool AreaBilling,
        decimal Rate,
        int Quantity,
        decimal Amount);


    // =========================================================
    // GENERATE
    // =========================================================

    /// <summary>
    /// Builds a report for the period (both dates inclusive).
    ///
    /// Cumulative: one row per plate type and rate.
    /// Detailed:   one row per client, challan no., plate type and rate.
    ///
    /// Lines that share a plate type but not a rate stay separate rows,
    /// so Quantity × Rate = Amount on every row.
    /// </summary>
    public async Task<Report> GenerateAsync(
        ReportType type,
        DateTime from,
        DateTime to,
        ReportClientOption client)
    {
        bool allClients = client.ClientId == null;

        var lines = await LoadLinesAsync(from, to, client.ClientId);

        var rows = type == ReportType.Detailed
            ? BuildDetailedRows(lines)
            : BuildCumulativeRows(lines);

        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = i + 1;
        }

        return new Report
        {
            Type = type,
            From = from.Date,
            To = to.Date,
            Title = allClients ? BusinessName : client.Name,
            Subtitle = BuildSubtitle(type, from, to, client),
            Columns = GetColumns(type, allClients),
            Rows = rows
        };
    }


    // =========================================================
    // DATA
    // =========================================================

    private static async Task<List<Line>> LoadLinesAsync(
        DateTime from,
        DateTime to,
        int? clientId)
    {
        using var db = new AppDbContext();

        DateTime start = from.Date;
        DateTime end = to.Date.AddDays(1); // exclusive upper bound

        var query = db.Challans
            .AsNoTracking()
            .Where(c => c.Date >= start && c.Date < end);

        if (clientId.HasValue)
        {
            query = query.Where(c => c.ClientId == clientId.Value);
        }

        // Read only the columns the report needs.
        var challans = await query
            .Select(c => new
            {
                ClientName = c.Client.Name,
                c.ChallanNo,
                PlateTypeCode = c.PlateType.Code,
                c.AreaBilling,
                c.AppliedRate,
                c.PlateLength,
                c.PlateBreadth,
                c.AppliedAreaRate,
                c.Quantity,
                c.TotalCost
            })
            .ToListAsync();

        return challans
            .Select(c => new Line(
                c.ClientName,
                c.ChallanNo,
                c.PlateTypeCode,
                c.AreaBilling,
                c.AreaBilling
                    ? Math.Round(c.PlateLength * c.PlateBreadth * c.AppliedAreaRate, 2)
                    : c.AppliedRate,
                c.Quantity,
                c.TotalCost))
            .ToList();
    }


    // =========================================================
    // ROWS
    // =========================================================

    private static List<ReportRow> BuildDetailedRows(List<Line> lines)
    {
        return lines
            .GroupBy(l => (l.ClientName, l.ChallanNo, l.PlateTypeCode, l.AreaBilling, l.Rate))
            .Select(g => ToRow(g, g.Key.ClientName, g.Key.ChallanNo))
            .OrderBy(r => r.ChallanNo)
            .ThenBy(r => r.ClientName)
            .ThenBy(r => r.PlateTypeCode)
            .ThenBy(r => r.AreaBilling)
            .ThenBy(r => r.Rate)
            .ToList();
    }

    private static List<ReportRow> BuildCumulativeRows(List<Line> lines)
    {
        return lines
            .GroupBy(l => (l.PlateTypeCode, l.AreaBilling, l.Rate))
            .Select(g => ToRow(g, clientName: string.Empty, challanNo: 0))
            .OrderBy(r => r.PlateTypeCode)
            .ThenBy(r => r.AreaBilling)
            .ThenBy(r => r.Rate)
            .ToList();
    }

    private static ReportRow ToRow(
        IEnumerable<Line> group,
        string clientName,
        int challanNo)
    {
        var lines = group.ToList();
        var first = lines[0];

        return new ReportRow
        {
            ClientName = clientName,
            ChallanNo = challanNo,
            PlateTypeCode = first.PlateTypeCode,
            AreaBilling = first.AreaBilling,
            Rate = first.Rate,
            Quantity = lines.Sum(l => l.Quantity),
            Amount = lines.Sum(l => l.Amount)
        };
    }


    // =========================================================
    // LAYOUT
    // =========================================================

    private static IReadOnlyList<ReportColumn> GetColumns(
        ReportType type,
        bool allClients)
    {
        var columns = new List<ReportColumn> { ReportColumn.SerialNo };

        if (type == ReportType.Detailed)
        {
            if (allClients)
            {
                columns.Add(ReportColumn.Client);
            }

            columns.Add(ReportColumn.ChallanNo);
        }

        columns.Add(ReportColumn.PlateType);
        columns.Add(ReportColumn.Quantity);
        columns.Add(ReportColumn.Rate);
        columns.Add(ReportColumn.Amount);

        return columns;
    }

    private static string BuildSubtitle(
        ReportType type,
        DateTime from,
        DateTime to,
        ReportClientOption client)
    {
        var parts = new List<string>();

        if (client.ClientId == null)
        {
            parts.Add("All clients");
        }
        else if (!string.IsNullOrWhiteSpace(client.Phone))
        {
            parts.Add($"Ph: {client.Phone}");
        }

        parts.Add($"{type} report");
        parts.Add($"{from:dd MMM yyyy} – {to:dd MMM yyyy}");

        return string.Join("   ·   ", parts);
    }
}
