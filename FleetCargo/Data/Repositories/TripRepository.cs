using FleetCargo.Models;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data.Repositories;

public class TripRepository : RepositoryBase, ITripRepository
{
    private const string Select = """
        SELECT t.trip_id, t.vehicle_id, t.driver_id, t.cargo_id, t.origin, t.destination,
               t.start_date, t.end_date, t.status,
               v.plate_number, d.last_name || ' ' || d.first_name, c.name
        FROM Trip t
        JOIN Vehicle v ON v.vehicle_id = t.vehicle_id
        JOIN Driver d ON d.driver_id = t.driver_id
        JOIN Cargo c ON c.cargo_id = t.cargo_id
        """;

    public TripRepository(IConnectionFactory factory) : base(factory) { }

    public IReadOnlyList<Trip> GetAll(UnitOfWork? uow = null) =>
        Query(Select + " ORDER BY t.start_date DESC, t.trip_id DESC", Map, uow);

    public Trip? GetById(int id, UnitOfWork? uow = null) =>
        QuerySingle(Select + " WHERE t.trip_id = $id", Map, uow, ("$id", id));

    /// <summary>Пошук рейсів за текстом, статусом і періодом (перетин з періодом).</summary>
    public IReadOnlyList<Trip> Search(TripFilter f, UnitOfWork? uow = null)
    {
        var where = new WhereBuilder();
        where.AddText(new[] { "t.origin", "t.destination", "v.plate_number",
                              "d.last_name", "d.first_name", "c.name" }, f.Text, f.ExactMatch);
        where.Add("t.status = $status", "$status", f.Status);
        where.Add("t.end_date >= $from", "$from", f.From.HasValue ? DbDates.ToDb(f.From.Value) : null);
        where.Add("t.start_date <= $to", "$to", f.To.HasValue ? DbDates.ToDb(f.To.Value) : null);

        return Query(Select + where.Sql + " ORDER BY t.start_date DESC, t.trip_id DESC",
            Map, uow, where.Parameters);
    }

    public int Add(Trip t, UnitOfWork? uow = null) => Insert("""
        INSERT INTO Trip (vehicle_id, driver_id, cargo_id, origin, destination,
                          start_date, end_date, status)
        VALUES ($vehicle, $driver, $cargo, $origin, $destination, $start, $end, $status)
        """, uow,
        ("$vehicle", t.VehicleId), ("$driver", t.DriverId), ("$cargo", t.CargoId),
        ("$origin", t.Origin), ("$destination", t.Destination),
        ("$start", DbDates.ToDb(t.StartDate)), ("$end", DbDates.ToDb(t.EndDate)),
        ("$status", t.Status));

    public void SetStatus(int id, string status, UnitOfWork? uow = null) =>
        Execute("UPDATE Trip SET status = $status WHERE trip_id = $id", uow,
            ("$status", status), ("$id", id));

    private static Trip Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        VehicleId = r.GetInt32(1),
        DriverId = r.GetInt32(2),
        CargoId = r.GetInt32(3),
        Origin = r.GetString(4),
        Destination = r.GetString(5),
        StartDate = DbDates.FromDb(r.GetString(6)),
        EndDate = DbDates.FromDb(r.GetString(7)),
        Status = r.GetString(8),
        VehicleName = r.GetString(9),
        DriverName = r.GetString(10),
        CargoName = r.GetString(11)
    };
}
