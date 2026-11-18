namespace FleetCargo.Models;

/// <summary>Автомобіль автопарку (таблиця Vehicle).</summary>
public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int Mileage { get; set; }
    public DateTime NextServiceDate { get; set; }
    public DateTime InsuranceEndDate { get; set; }
    public string Status { get; set; } = ResourceStatus.Free;

    /// <summary>Ім'я файлу фото в каталозі Photos (null, якщо фото немає).</summary>
    public string? PhotoPath { get; set; }

    /// <summary>Технічні примітки та опис автомобіля.</summary>
    public string Notes { get; set; } = "";

    public string Title => $"{Brand} {Model}";

    /// <summary>Рівень попередження; обчислюється сервісом попереджень, у базі не зберігається.</summary>
    public AlertLevel AlertLevel { get; set; } = AlertLevel.None;

    public override string ToString() => $"{PlateNumber} ({Brand} {Model})";
}
