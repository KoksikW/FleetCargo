using FleetCargo.Models;

namespace FleetCargo.Data.Repositories;

public interface IVehicleRepository
{
    IReadOnlyList<Vehicle> GetAll(UnitOfWork? uow = null);
    Vehicle? GetById(int id, UnitOfWork? uow = null);
    IReadOnlyList<Vehicle> GetAvailable(DateTime start, DateTime end, UnitOfWork? uow = null);
    IReadOnlyList<Vehicle> Search(VehicleFilter filter, UnitOfWork? uow = null);
    int Add(Vehicle vehicle, UnitOfWork? uow = null);
    void Update(Vehicle vehicle, UnitOfWork? uow = null);
    void Delete(int id, UnitOfWork? uow = null);
    void SetStatus(int id, string status, UnitOfWork? uow = null);
}
