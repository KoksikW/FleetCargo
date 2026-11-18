namespace FleetCargo.Models;

/// <summary>Вантаж (таблиця Cargo).</summary>
public class Cargo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double WeightKg { get; set; }
    public int CargoTypeId { get; set; }

    /// <summary>Назва типу вантажу (береться з таблиці CargoType під час читання).</summary>
    public string CargoTypeName { get; set; } = "";

    public override string ToString() => $"{Name} ({WeightKg:0.##} кг)";
}
