using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>
/// Спільна логіка репозиторіїв: виконання параметризованих запитів на власному підключенні
/// або в межах переданої транзакції (UnitOfWork).
/// </summary>
public abstract class RepositoryBase
{
    private readonly IConnectionFactory _factory;

    protected RepositoryBase(IConnectionFactory factory) => _factory = factory;

    /// <summary>Виконує SELECT і перетворює кожен рядок результату на об'єкт.</summary>
    protected List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, UnitOfWork? uow,
        params (string Name, object? Value)[] args)
    {
        return Run(uow, (connection, transaction) =>
        {
            using SqliteCommand command = CreateCommand(connection, transaction, sql, args);
            using SqliteDataReader reader = command.ExecuteReader();
            var result = new List<T>();
            while (reader.Read())
                result.Add(map(reader));
            return result;
        });
    }

    /// <summary>Виконує SELECT і повертає перший рядок або null.</summary>
    protected T? QuerySingle<T>(string sql, Func<SqliteDataReader, T> map, UnitOfWork? uow,
        params (string Name, object? Value)[] args) where T : class
    {
        return Query(sql, map, uow, args).FirstOrDefault();
    }

    /// <summary>Виконує INSERT, UPDATE або DELETE; повертає кількість змінених рядків.</summary>
    protected int Execute(string sql, UnitOfWork? uow, params (string Name, object? Value)[] args)
    {
        return Run(uow, (connection, transaction) =>
        {
            using SqliteCommand command = CreateCommand(connection, transaction, sql, args);
            return command.ExecuteNonQuery();
        });
    }

    /// <summary>Виконує INSERT і повертає ідентифікатор доданого рядка.</summary>
    protected int Insert(string sql, UnitOfWork? uow, params (string Name, object? Value)[] args)
    {
        return Run(uow, (connection, transaction) =>
        {
            using SqliteCommand command = CreateCommand(
                connection, transaction, sql + "; SELECT last_insert_rowid();", args);
            return Convert.ToInt32(command.ExecuteScalar());
        });
    }

    private T Run<T>(UnitOfWork? uow, Func<SqliteConnection, SqliteTransaction?, T> action)
    {
        if (uow != null)
            return action(uow.Connection, uow.Transaction);

        using SqliteConnection connection = _factory.CreateConnection();
        connection.Open();
        return action(connection, null);
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection,
        SqliteTransaction? transaction, string sql, (string Name, object? Value)[] args)
    {
        SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in args)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }
}
