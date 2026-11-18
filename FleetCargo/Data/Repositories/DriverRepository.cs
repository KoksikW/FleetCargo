using FleetCargo.Models;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data.Repositories;

public class DriverRepository : RepositoryBase, IDriverRepository
{
    private const string Columns = "driver_id, last_name, first_name, phone, status, photo_path, notes";

    public DriverRepository(IConnectionFactory factory) : base(factory) { }

    public IReadOnlyList<Driver> GetAll(UnitOfWork? uow = null) =>
        Query($"SELECT {Columns} FROM Driver ORDER BY last_name, first_name", Map, uow);

    public Driver? GetById(int id, UnitOfWork? uow = null) =>
        QuerySingle($"SELECT {Columns} FROM Driver WHERE driver_id = $id", Map, uow, ("$id", id));

    /// <summary>Вільні водії на період: статус «Вільний» і немає незавершених рейсів, що перетинаються з періодом.</summary>
    public IReadOnlyList<Driver> GetAvailable(DateTime start, DateTime end, UnitOfWork? uow = null) =>
        Query("""
            SELECT d.driver_id, d.last_name, d.first_name, d.phone, d.status, d.photo_path, d.notes
            FROM Driver d
            WHERE d.status = $free
              AND NOT EXISTS (
                  SELECT 1 FROM Trip t
                  WHERE t.driver_id = d.driver_id
                    AND t.status <> $completed
                    AND t.start_date <= $end
                    AND t.end_date >= $start)
            ORDER BY d.last_name, d.first_name
            """, Map, uow,
            ("$free", ResourceStatus.Free), ("$completed", TripStatus.Completed),
            ("$start", DbDates.ToDb(start)), ("$end", DbDates.ToDb(end)));

    /// <summary>Пошук водіїв за заповненими критеріями.</summary>
    public IReadOnlyList<Driver> Search(DriverFilter f, UnitOfWork? uow = null)
    {
        var where = new WhereBuilder();
        where.AddText(new[] { "last_name", "first_name", "phone" }, f.Text, f.ExactMatch);
        where.Add("status = $status", "$status", f.Status);

        return Query($"SELECT {Columns} FROM Driver{where.Sql} ORDER BY last_name, first_name",
            Map, uow, where.Parameters);
    }

    public int Add(Driver d, UnitOfWork? uow = null) => Insert("""
        INSERT INTO Driver (last_name, first_name, phone, status, photo_path, notes)
        VALUES ($last, $first, $phone, $status, $photo, $notes)
        """, uow,
        ("$last", d.LastName), ("$first", d.FirstName), ("$phone", d.Phone), ("$status", d.Status),
        ("$photo", d.PhotoPath), ("$notes", d.Notes));

    public void Update(Driver d, UnitOfWork? uow = null) => Execute("""
        UPDATE Driver
        SET last_name = $last, first_name = $first, phone = $phone, status = $status,
            photo_path = $photo, notes = $notes
        WHERE driver_id = $id
        """, uow,
        ("$id", d.Id), ("$last", d.LastName), ("$first", d.FirstName),
        ("$phone", d.Phone), ("$status", d.Status), ("$photo", d.PhotoPath), ("$notes", d.Notes));

    public void Delete(int id, UnitOfWork? uow = null) =>
        Execute("DELETE FROM Driver WHERE driver_id = $id", uow, ("$id", id));

    public void SetStatus(int id, string status, UnitOfWork? uow = null) =>
        Execute("UPDATE Driver SET status = $status WHERE driver_id = $id", uow,
            ("$status", status), ("$id", id));

    private static Driver Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        LastName = r.GetString(1),
        FirstName = r.GetString(2),
        Phone = r.GetString(3),
        Status = r.GetString(4),
        PhotoPath = r.IsDBNull(5) ? null : r.GetString(5),
        Notes = r.IsDBNull(6) ? "" : r.GetString(6)
    };
}
