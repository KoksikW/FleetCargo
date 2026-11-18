using System.Data;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;

namespace FleetCargo.Services;

/// <summary>Результат формування звіту: таблиця для показу та експорту й підсумковий рядок.</summary>
public class ReportResult
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public DataTable? Table { get; init; }
    public string Summary { get; init; } = "";
}

/// <summary>Формування звітів «Рейси за період» та «Автомобілі, що потребують ТО або страхування».</summary>
public class ReportService
{
    public const string TripsReportName = "Рейси за період";
    public const string AlertsReportName = "Автомобілі, що потребують ТО або страхування";

    public static readonly string[] ReportNames = { TripsReportName, AlertsReportName };

    private readonly ITripRepository _trips;
    private readonly IVehicleRepository _vehicles;
    private readonly AlertService _alerts;

    public ReportService(ITripRepository trips, IVehicleRepository vehicles, AlertService alerts)
    {
        _trips = trips;
        _vehicles = vehicles;
        _alerts = alerts;
    }

    public ReportResult Build(string reportName, DateTime? from, DateTime? to)
    {
        return reportName switch
        {
            TripsReportName => BuildTripsReport(from, to),
            AlertsReportName => BuildAlertsReport(),
            _ => new ReportResult { Success = false, Error = "Невідомий тип звіту." }
        };
    }

    private ReportResult BuildTripsReport(DateTime? from, DateTime? to)
    {
        if (from != null && to != null && from > to)
            return new ReportResult { Success = false, Error = "Початок періоду пізніший за його кінець." };

        IReadOnlyList<Trip> trips = _trips.Search(new TripFilter { From = from, To = to });

        var table = new DataTable("Report");
        foreach (string column in new[] { "№", "Автомобіль", "Водій", "Вантаж", "Звідки", "Куди", "Початок", "Кінець", "Статус" })
            table.Columns.Add(column, typeof(string));

        foreach (Trip t in trips)
        {
            table.Rows.Add(t.Id.ToString(), t.VehicleName, t.DriverName, t.CargoName,
                t.Origin, t.Destination, t.StartDate.ToString("dd.MM.yyyy"),
                t.EndDate.ToString("dd.MM.yyyy"), t.Status);
        }

        int inProgress = trips.Count(t => t.Status == TripStatus.InProgress);
        return new ReportResult
        {
            Success = true,
            Table = table,
            Summary = $"Рейсів: {trips.Count}; у дорозі: {inProgress}; завершених: {trips.Count - inProgress}"
        };
    }

    private ReportResult BuildAlertsReport()
    {
        List<AlertItem> items = _alerts.BuildAlerts(_vehicles.GetAll(), DateTime.Today);

        var table = new DataTable("Report");
        foreach (string column in new[] { "Автомобіль", "Що перевіряється", "Дата", "Днів до терміну", "Стан" })
            table.Columns.Add(column, typeof(string));

        foreach (AlertItem a in items)
        {
            table.Rows.Add($"{a.Vehicle.PlateNumber} ({a.Vehicle.Title})", a.Kind,
                a.Date.ToString("dd.MM.yyyy"),
                a.Days < 0 ? $"прострочено на {-a.Days}" : a.Days.ToString(),
                a.Days < 0 ? "Прострочено" : "Скоро спливає");
        }

        int expired = items.Count(a => a.Days < 0);
        return new ReportResult
        {
            Success = true,
            Table = table,
            Summary = $"Попереджень: {items.Count}; прострочених: {expired}"
        };
    }
}
