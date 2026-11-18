using FleetCargo.Models;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data.Repositories;

public class VehicleRepository : RepositoryBase, IVehicleRepository
{
    private const string Columns =
        "vehicle_id, plate_number, brand, model, mileage_km, next_service_date, insurance_end_date, status, photo_path, notes";

    public VehicleRepository(IConnectionFactory factory) : base(factory) { }

    public IReadOnlyList<Vehicle> GetAll(UnitOfWork? uow = null) =>
        Query($"SELECT {Columns} FROM Vehicle ORDER BY plate_number", Map, uow);

    public Vehicle? GetById(int id, UnitOfWork? uow = null) =>
        QuerySingle($"SELECT {Columns} FROM Vehicle WHERE vehicle_id = $id", Map, uow, ("$id", id));

    /// <summary>Вільні автомобілі на період: статус «Вільний» і немає незавершених рейсів, що перетинаються з періодом.</summary>
    public IReadOnlyList<Vehicle> GetAvailable(DateTime start, DateTime end, UnitOfWork? uow = null) =>
        Query("""
            SELECT v.vehicle_id, v.plate_number, v.brand, v.model, v.mileage_km,
                   v.next_service_date, v.insurance_end_date, v.status, v.photo_path, v.notes
            FROM Vehicle v
            WHERE v.status = $free
              AND NOT EXISTS (
                  SELECT 1 FROM Trip t
                  WHERE t.vehicle_id = v.vehicle_id
                    AND t.status <> $completed
                    AND t.start_date <= $end
                    AND t.end_date >= $start)
            ORDER BY v.plate_number
            """, Map, uow,
            ("$free", ResourceStatus.Free), ("$completed", TripStatus.Completed),
            ("$start", DbDates.ToDb(start)), ("$end", DbDates.ToDb(end)));

    /// <summary>Пошук автомобілів за заповненими критеріями (динамічний параметризований запит).</summary>
    public IReadOnlyList<Vehicle> Search(VehicleFilter f, UnitOfWork? uow = null)
    {
        var where = new WhereBuilder();
        where.AddText(new[] { "plate_number", "brand", "model" }, f.Text, f.ExactMatch);
        where.Add("status = $status", "$status", f.Status);
        where.Add("mileage_km >= $mileageFrom", "$mileageFrom", f.MileageFrom);
        where.Add("mileage_km <= $mileageTo", "$mileageTo", f.MileageTo);
        where.Add("next_service_date <= $serviceBefore", "$serviceBefore",
            f.ServiceBefore.HasValue ? DbDates.ToDb(f.ServiceBefore.Value) : null);

        return Query($"SELECT {Columns} FROM Vehicle{where.Sql} ORDER BY plate_number",
            Map, uow, where.Parameters);
    }

    public int Add(Vehicle v, UnitOfWork? uow = null) => Insert("""
        INSERT INTO Vehicle (plate_number, brand, model, mileage_km,
                             next_service_date, insurance_end_date, status, photo_path, notes)
        VALUES ($plate, $brand, $model, $mileage, $service, $insurance, $status, $photo, $notes)
        """, uow,
        ("$plate", v.PlateNumber), ("$brand", v.Brand), ("$model", v.Model),
        ("$mileage", v.Mileage), ("$service", DbDates.ToDb(v.NextServiceDate)),
        ("$insurance", DbDates.ToDb(v.InsuranceEndDate)), ("$status", v.Status),
        ("$photo", v.PhotoPath), ("$notes", v.Notes));

    public void Update(Vehicle v, UnitOfWork? uow = null) => Execute("""
        UPDATE Vehicle
        SET plate_number = $plate, brand = $brand, model = $model, mileage_km = $mileage,
            next_service_date = $service, insurance_end_date = $insurance, status = $status,
            photo_path = $photo, notes = $notes
        WHERE vehicle_id = $id
        """, uow,
        ("$id", v.Id), ("$plate", v.PlateNumber), ("$brand", v.Brand), ("$model", v.Model),
        ("$mileage", v.Mileage), ("$service", DbDates.ToDb(v.NextServiceDate)),
        ("$insurance", DbDates.ToDb(v.InsuranceEndDate)), ("$status", v.Status),
        ("$photo", v.PhotoPath), ("$notes", v.Notes));

    public void Delete(int id, UnitOfWork? uow = null) =>
        Execute("DELETE FROM Vehicle WHERE vehicle_id = $id", uow, ("$id", id));

    public void SetStatus(int id, string status, UnitOfWork? uow = null) =>
        Execute("UPDATE Vehicle SET status = $status WHERE vehicle_id = $id", uow,
            ("$status", status), ("$id", id));

    private static Vehicle Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        PlateNumber = r.GetString(1),
        Brand = r.GetString(2),
        Model = r.GetString(3),
        Mileage = r.GetInt32(4),
        NextServiceDate = DbDates.FromDb(r.GetString(5)),
        InsuranceEndDate = DbDates.FromDb(r.GetString(6)),
        Status = r.GetString(7),
        PhotoPath = r.IsDBNull(8) ? null : r.GetString(8),
        Notes = r.IsDBNull(9) ? "" : r.GetString(9)
    };
}
