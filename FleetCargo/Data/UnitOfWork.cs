using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>
/// Одна транзакція для кількох операцій: усі зміни зберігаються методом Commit
/// або скасовуються, якщо об'єкт знищено без виклику Commit.
/// </summary>
public sealed class UnitOfWork : IDisposable
{
    public SqliteConnection Connection { get; }
    public SqliteTransaction Transaction { get; }

    public UnitOfWork(IConnectionFactory factory)
    {
        Connection = factory.CreateConnection();
        Connection.Open();
        Transaction = Connection.BeginTransaction();
    }

    public void Commit() => Transaction.Commit();

    public void Dispose()
    {
        Transaction.Dispose();   // без Commit транзакція скасовується
        Connection.Dispose();
    }
}
