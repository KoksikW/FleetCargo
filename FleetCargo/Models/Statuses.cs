namespace FleetCargo.Models;

/// <summary>Статуси автомобіля та водія.</summary>
public static class ResourceStatus
{
    public const string Free = "Вільний";
    public const string OnTrip = "В дорозі";
}

/// <summary>Статуси рейсу.</summary>
public static class TripStatus
{
    public const string InProgress = "В дорозі";
    public const string Completed = "Завершено";
}
