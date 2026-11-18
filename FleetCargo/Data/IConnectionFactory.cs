using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>Створює підключення до бази даних.</summary>
public interface IConnectionFactory
{
    /// <summary>Повертає нове, ще не відкрите підключення.</summary>
    SqliteConnection CreateConnection();
}
