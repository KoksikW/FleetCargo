using FleetCargo.Models;

namespace FleetCargo.Data.Repositories;

public interface ITripRepository
{
    IReadOnlyList<Trip> GetAll(UnitOfWork? uow = null);
    Trip? GetById(int id, UnitOfWork? uow = null);
    IReadOnlyList<Trip> Search(TripFilter filter, UnitOfWork? uow = null);
    int Add(Trip trip, UnitOfWork? uow = null);
    void SetStatus(int id, string status, UnitOfWork? uow = null);
}
