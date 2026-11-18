using System.Data;
using ClosedXML.Excel;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;

namespace FleetCargo.Services;

/// <summary>Експорт маршрутного листа та звітів у файли Excel (бібліотека ClosedXML).</summary>
public class ExportService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;
    private readonly ICargoRepository _cargos;
    private readonly ITripRepository _trips;

    public ExportService(IVehicleRepository vehicles, IDriverRepository drivers,
        ICargoRepository cargos, ITripRepository trips)
    {
        _vehicles = vehicles;
        _drivers = drivers;
        _cargos = cargos;
        _trips = trips;
    }

    /// <summary>Формує маршрутний лист рейсу у файлі Excel (алгоритм А5).</summary>
    public OperationResult ExportRouteSheet(int tripId, string path)
    {
        Trip? trip = _trips.GetById(tripId);
        if (trip == null)
            return OperationResult.Fail("Рейс не знайдено");

        Vehicle? vehicle = _vehicles.GetById(trip.VehicleId);
        Driver? driver = _drivers.GetById(trip.DriverId);
        Cargo? cargo = _cargos.GetById(trip.CargoId);
        if (vehicle == null || driver == null || cargo == null)
            return OperationResult.Fail("Не знайдено дані автомобіля, водія або вантажу");

        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.Worksheets.Add("Маршрутний лист");

        sheet.Cell(1, 1).Value = $"МАРШРУТНИЙ ЛИСТ № {trip.Id}";
        IXLRange title = sheet.Range(1, 1, 1, 2).Merge();
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[,] rows =
        {
            { "Автомобіль", $"{vehicle.PlateNumber} ({vehicle.Brand} {vehicle.Model})" },
            { "Водій", driver.FullName },
            { "Телефон водія", driver.Phone },
            { "Вантаж", $"{cargo.Name}, {cargo.WeightKg:0.##} кг ({cargo.CargoTypeName})" },
            { "Пункт відправлення", trip.Origin },
            { "Пункт призначення", trip.Destination },
            { "Дата початку", trip.StartDate.ToString("dd.MM.yyyy") },
            { "Дата завершення", trip.EndDate.ToString("dd.MM.yyyy") },
            { "Статус рейсу", trip.Status },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            sheet.Cell(3 + i, 1).Value = rows[i, 0];
            sheet.Cell(3 + i, 2).Value = rows[i, 1];
        }

        int last = 2 + rows.GetLength(0);
        IXLRange body = sheet.Range(3, 1, last, 2);
        body.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        body.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(3, 1, last, 1).Style.Font.Bold = true;

        sheet.Cell(last + 2, 1).Value = "Диспетчер: ____________";
        sheet.Cell(last + 2, 2).Value = "Водій: ____________";

        sheet.Column(1).Width = 24;
        sheet.Column(2).Width = 56;
        workbook.SaveAs(path);
        return OperationResult.Ok(trip.Id);
    }

    /// <summary>Зберігає таблицю звіту у файл Excel.</summary>
    public void ExportTable(string title, DataTable table, string path)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.Worksheets.Add(table, "Звіт");
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }
}
