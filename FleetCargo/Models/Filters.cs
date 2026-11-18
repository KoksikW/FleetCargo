namespace FleetCargo.Models;

/// <summary>Критерії пошуку автомобілів. Порожні (null) критерії не враховуються.</summary>
public class VehicleFilter
{
    public string? Text { get; set; }          // номер, марка або модель
    public bool ExactMatch { get; set; }       // false – містить, true – точний збіг
    public string? Status { get; set; }
    public int? MileageFrom { get; set; }
    public int? MileageTo { get; set; }
    public DateTime? ServiceBefore { get; set; }   // наступне ТО не пізніше цієї дати
}

/// <summary>Критерії пошуку водіїв.</summary>
public class DriverFilter
{
    public string? Text { get; set; }          // прізвище, ім'я або телефон
    public bool ExactMatch { get; set; }
    public string? Status { get; set; }
}

/// <summary>Критерії пошуку рейсів.</summary>
public class TripFilter
{
    public string? Text { get; set; }          // маршрут, автомобіль, водій або вантаж
    public bool ExactMatch { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }        // рейси, що перетинаються з періодом [From; To]
    public DateTime? To { get; set; }
}
