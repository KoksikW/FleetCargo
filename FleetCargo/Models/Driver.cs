namespace FleetCargo.Models;

/// <summary>Водій (таблиця Driver).</summary>
public class Driver
{
    public int Id { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Status { get; set; } = ResourceStatus.Free;

    /// <summary>Ім'я файлу фото в каталозі Photos (null, якщо фото немає).</summary>
    public string? PhotoPath { get; set; }

    /// <summary>Текстова характеристика водія.</summary>
    public string Notes { get; set; } = "";

    public string FullName => $"{LastName} {FirstName}";

    public override string ToString() => FullName;
}
