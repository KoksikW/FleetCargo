using FleetCargo.Data;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;

namespace FleetCargo.Services;

/// <summary>Створення та завершення рейсів (алгоритми А1 і А4).</summary>
public class TripService
{
    private readonly IConnectionFactory _factory;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;
    private readonly ITripRepository _trips;

    public TripService(IConnectionFactory factory, IVehicleRepository vehicles,
        IDriverRepository drivers, ITripRepository trips)
    {
        _factory = factory;
        _vehicles = vehicles;
        _drivers = drivers;
        _trips = trips;
    }

    /// <summary>
    /// Створює рейс і змінює статуси автомобіля та водія в одній транзакції.
    /// Якщо передано external, використовується зовнішня транзакція (без підтвердження).
    /// </summary>
    public OperationResult CreateTrip(int vehicleId, int driverId, int cargoId,
        string origin, string destination, DateTime start, DateTime end,
        UnitOfWork? external = null)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) || start > end)
            return OperationResult.Fail("Некоректні дані рейсу");

        UnitOfWork? own = external == null ? new UnitOfWork(_factory) : null;
        UnitOfWork uow = external ?? own!;
        try
        {
            Vehicle? vehicle = _vehicles.GetById(vehicleId, uow);
            Driver? driver = _drivers.GetById(driverId, uow);
            if (vehicle == null || driver == null)
                return OperationResult.Fail("Автомобіль або водія не знайдено");

            if (vehicle.Status == ResourceStatus.OnTrip || driver.Status == ResourceStatus.OnTrip)
                return OperationResult.Fail("Ресурс уже в дорозі");

            // Автомобіль не можна відправити в рейс, якщо ТО або страхування закінчаться
            // раніше за завершення рейсу (або вже прострочені)
            if (vehicle.NextServiceDate.Date < end.Date)
                return OperationResult.Fail(
                    $"Рейс неможливий: наступне ТО автомобіля {vehicle.PlateNumber} призначено на " +
                    $"{vehicle.NextServiceDate:dd.MM.yyyy}, що раніше за завершення рейсу " +
                    $"({end:dd.MM.yyyy}). Спочатку виконайте ТО.");
            if (vehicle.InsuranceEndDate.Date < end.Date)
                return OperationResult.Fail(
                    $"Рейс неможливий: страхування автомобіля {vehicle.PlateNumber} діє до " +
                    $"{vehicle.InsuranceEndDate:dd.MM.yyyy}, що раніше за завершення рейсу " +
                    $"({end:dd.MM.yyyy}). Оновіть страхування.");

            bool vehicleFree = _vehicles.GetAvailable(start, end, uow).Any(v => v.Id == vehicleId);
            bool driverFree = _drivers.GetAvailable(start, end, uow).Any(d => d.Id == driverId);
            if (!vehicleFree || !driverFree)
                return OperationResult.Fail("Ресурс зайнятий на обрані дати");

            int tripId = _trips.Add(new Trip
            {
                VehicleId = vehicleId,
                DriverId = driverId,
                CargoId = cargoId,
                Origin = origin.Trim(),
                Destination = destination.Trim(),
                StartDate = start,
                EndDate = end,
                Status = TripStatus.InProgress
            }, uow);
            _vehicles.SetStatus(vehicleId, ResourceStatus.OnTrip, uow);
            _drivers.SetStatus(driverId, ResourceStatus.OnTrip, uow);

            own?.Commit();
            return OperationResult.Ok(tripId);
        }
        finally
        {
            own?.Dispose();
        }
    }

    /// <summary>Завершує рейс і повертає автомобіль та водія у статус «Вільний».</summary>
    public OperationResult CompleteTrip(int tripId, UnitOfWork? external = null)
    {
        UnitOfWork? own = external == null ? new UnitOfWork(_factory) : null;
        UnitOfWork uow = external ?? own!;
        try
        {
            Trip? trip = _trips.GetById(tripId, uow);
            if (trip == null)
                return OperationResult.Fail("Рейс не знайдено");
            if (trip.Status == TripStatus.Completed)
                return OperationResult.Fail("Рейс уже завершено");

            _trips.SetStatus(tripId, TripStatus.Completed, uow);
            _vehicles.SetStatus(trip.VehicleId, ResourceStatus.Free, uow);
            _drivers.SetStatus(trip.DriverId, ResourceStatus.Free, uow);

            own?.Commit();
            return OperationResult.Ok(tripId);
        }
        finally
        {
            own?.Dispose();
        }
    }
}
