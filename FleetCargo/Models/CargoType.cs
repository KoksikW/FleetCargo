namespace FleetCargo.Models;

/// <summary>Тип вантажу (таблиця CargoType).</summary>
public class CargoType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public override string ToString() => Name;
}
