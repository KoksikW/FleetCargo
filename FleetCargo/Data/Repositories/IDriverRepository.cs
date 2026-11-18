using FleetCargo.Models;

namespace FleetCargo.Data.Repositories;

public interface IDriverRepository
{
    IReadOnlyList<Driver> GetAll(UnitOfWork? uow = null);
    Driver? GetById(int id, UnitOfWork? uow = null);
    IReadOnlyList<Driver> GetAvailable(DateTime start, DateTime end, UnitOfWork? uow = null);
    IReadOnlyList<Driver> Search(DriverFilter filter, UnitOfWork? uow = null);
    int Add(Driver driver, UnitOfWork? uow = null);
    void Update(Driver driver, UnitOfWork? uow = null);
    void Delete(int id, UnitOfWork? uow = null);
    void SetStatus(int id, string status, UnitOfWork? uow = null);
}
