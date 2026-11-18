using FleetCargo.Models;

namespace FleetCargo.Data.Repositories;

public interface ICargoRepository
{
    IReadOnlyList<Cargo> GetAll(UnitOfWork? uow = null);
    Cargo? GetById(int id, UnitOfWork? uow = null);
    IReadOnlyList<CargoType> GetTypes(UnitOfWork? uow = null);
    int AddType(CargoType type, UnitOfWork? uow = null);
    int Add(Cargo cargo, UnitOfWork? uow = null);
    void Update(Cargo cargo, UnitOfWork? uow = null);
    void Delete(int id, UnitOfWork? uow = null);
}
