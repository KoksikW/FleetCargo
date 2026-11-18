using FleetCargo.Data.Repositories;
using FleetCargo.Models;

namespace FleetCargo.Services;

public class AvailabilityResult
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public IReadOnlyList<Vehicle> Vehicles { get; init; } = Array.Empty<Vehicle>();
    public IReadOnlyList<Driver> Drivers { get; init; } = Array.Empty<Driver>();
}

/// <summary>Перевірка доступності автомобілів і водіїв на дати (алгоритм А2).</summary>
public class AvailabilityService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;

    public AvailabilityService(IVehicleRepository vehicles, IDriverRepository drivers)
    {
        _vehicles = vehicles;
        _drivers = drivers;
    }

    public AvailabilityResult GetAvailable(DateTime start, DateTime end)
    {
        if (start > end)
            return new AvailabilityResult { Success = false, Error = "Некоректний період: дата початку пізніша за дату завершення." };

        return new AvailabilityResult
        {
            Success = true,
            Vehicles = _vehicles.GetAvailable(start, end),
            Drivers = _drivers.GetAvailable(start, end)
        };
    }
}
