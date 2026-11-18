namespace FleetCargo.Models;

/// <summary>Рейс (таблиця Trip).</summary>
public class Trip
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int CargoId { get; set; }
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = TripStatus.InProgress;

    // Поля для відображення (заповнюються запитом із об'єднанням таблиць)
    public string VehicleName { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string CargoName { get; set; } = "";
}
