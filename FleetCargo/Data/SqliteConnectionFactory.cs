using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>Формує рядок підключення з параметрів DbSettings і створює підключення.</summary>
public class SqliteConnectionFactory : IConnectionFactory
{
    public static string BuildConnectionString() => new SqliteConnectionStringBuilder
    {
        DataSource = DbSettings.DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = DbSettings.ForeignKeys,
        DefaultTimeout = DbSettings.TimeoutSeconds
    }.ToString();

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(BuildConnectionString());

        // Вбудований LIKE у SQLite не ігнорує регістр кирилиці, тому для пошуку
        // реєструється власна функція приведення тексту до нижнього регістру.
        connection.CreateFunction("ulower", (string? text) => text?.ToLowerInvariant(),
            isDeterministic: true);
        return connection;
    }
}
