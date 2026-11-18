using FleetCargo.Models;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data.Repositories;

public class CargoRepository : RepositoryBase, ICargoRepository
{
    private const string Select = """
        SELECT c.cargo_id, c.name, c.weight_kg, c.cargo_type_id, t.name
        FROM Cargo c
        JOIN CargoType t ON t.cargo_type_id = c.cargo_type_id
        """;

    public CargoRepository(IConnectionFactory factory) : base(factory) { }

    public IReadOnlyList<Cargo> GetAll(UnitOfWork? uow = null) =>
        Query(Select + " ORDER BY c.name", Map, uow);

    public Cargo? GetById(int id, UnitOfWork? uow = null) =>
        QuerySingle(Select + " WHERE c.cargo_id = $id", Map, uow, ("$id", id));

    public IReadOnlyList<CargoType> GetTypes(UnitOfWork? uow = null) =>
        Query("SELECT cargo_type_id, name FROM CargoType ORDER BY name",
            r => new CargoType { Id = r.GetInt32(0), Name = r.GetString(1) }, uow);

    public int AddType(CargoType type, UnitOfWork? uow = null) =>
        Insert("INSERT INTO CargoType (name) VALUES ($name)", uow, ("$name", type.Name));

    public int Add(Cargo c, UnitOfWork? uow = null) => Insert("""
        INSERT INTO Cargo (name, weight_kg, cargo_type_id)
        VALUES ($name, $weight, $type)
        """, uow,
        ("$name", c.Name), ("$weight", c.WeightKg), ("$type", c.CargoTypeId));

    public void Update(Cargo c, UnitOfWork? uow = null) => Execute("""
        UPDATE Cargo
        SET name = $name, weight_kg = $weight, cargo_type_id = $type
        WHERE cargo_id = $id
        """, uow,
        ("$id", c.Id), ("$name", c.Name), ("$weight", c.WeightKg), ("$type", c.CargoTypeId));

    public void Delete(int id, UnitOfWork? uow = null) =>
        Execute("DELETE FROM Cargo WHERE cargo_id = $id", uow, ("$id", id));

    private static Cargo Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        WeightKg = r.GetDouble(2),
        CargoTypeId = r.GetInt32(3),
        CargoTypeName = r.GetString(4)
    };
}
