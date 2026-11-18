using System.IO;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>
/// Створює файл бази даних SQLite, таблиці та індекси, а під час першого запуску
/// заповнює базу тестовими даними.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>Створює базу даних, якщо її немає, і заповнює тестовими даними.</summary>
    public static void Initialize()
    {
        Directory.CreateDirectory(DbSettings.DatabaseFolder);

        using SqliteConnection connection = new SqliteConnectionFactory().CreateConnection();
        connection.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();

        Execute(connection, transaction, DatabaseSchema.CreateTables);
        Execute(connection, transaction, DatabaseSchema.CreateIndexes);
        ApplyMigrations(connection, transaction);

        if (IsEmpty(connection, transaction))
            SeedTestData(connection, transaction);

        transaction.Commit();
    }

    /// <summary>Додає до таблиць нові поля, яких ще немає (повторний запуск нічого не змінює).</summary>
    private static void ApplyMigrations(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var (table, column, definition) in DatabaseSchema.Migrations)
        {
            if (!ColumnExists(connection, transaction, table, column))
                Execute(connection, transaction, $"ALTER TABLE {table} ADD COLUMN {column} {definition};");
        }
    }

    private static bool ColumnExists(SqliteConnection connection, SqliteTransaction transaction,
        string table, string column)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $name";
        command.Parameters.AddWithValue("$name", column);
        return (long)command.ExecuteScalar()! > 0;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>Вставляє рядок і повертає його згенерований ідентифікатор.</summary>
    private static long Insert(SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object Value)[] args)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql + "; SELECT last_insert_rowid();";
        foreach (var (name, value) in args)
            command.Parameters.AddWithValue(name, value);
        return (long)command.ExecuteScalar()!;
    }

    private static bool IsEmpty(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM Vehicle) + (SELECT COUNT(*) FROM Driver)
                 + (SELECT COUNT(*) FROM CargoType) + (SELECT COUNT(*) FROM Cargo)
                 + (SELECT COUNT(*) FROM Trip);
            """;
        return (long)command.ExecuteScalar()! == 0;
    }

    private static void SeedTestData(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Дати зберігаються у форматі ISO 8601 (рррр-мм-дд), бо SQLite не має окремого типу дати.
        string D(int daysFromToday) => DateTime.Today.AddDays(daysFromToday).ToString("yyyy-MM-dd");

        // Автомобілі: терміни ТО і страхування підібрано так, щоб були присутні
        // нормальні, близькі (до 14 днів) та прострочені значення.
        var vehicles = new (string Plate, string Brand, string Model, int Mileage,
                            int ServiceInDays, int InsuranceInDays, string Status)[]
        {
            ("АА 1234 ВХ", "MAN",           "TGX 18.510",   245600, 120, 200, "В дорозі"),
            ("КА 5678 ЕР", "Mercedes-Benz", "Actros 1845",  312400,  10, 150, "Вільний"),
            ("АЕ 9012 ТМ", "Volvo",         "FH 460",       198700,  -3,  50, "Вільний"),
            ("ВС 3456 ІК", "DAF",           "XF 480",       276300,  60,   9, "Вільний"),
            ("АХ 7890 НО", "Renault",       "Master",        84200,  90, 300, "Вільний"),
            ("ВІ 2468 СТ", "Ford",          "Transit",      121900,  45,  75, "Вільний"),
        };
        var vehicleIds = new List<long>();
        foreach (var v in vehicles)
        {
            vehicleIds.Add(Insert(connection, transaction, """
                INSERT INTO Vehicle (plate_number, brand, model, mileage_km,
                                     next_service_date, insurance_end_date, status)
                VALUES ($plate, $brand, $model, $mileage, $service, $insurance, $status)
                """,
                ("$plate", v.Plate), ("$brand", v.Brand), ("$model", v.Model),
                ("$mileage", v.Mileage), ("$service", D(v.ServiceInDays)),
                ("$insurance", D(v.InsuranceInDays)), ("$status", v.Status)));
        }

        // Водії
        var drivers = new (string Last, string First, string Phone, string Status)[]
        {
            ("Мельник",    "Олександр", "+380 50 123 45 67", "В дорозі"),
            ("Коваленко",  "Андрій",    "+380 67 234 56 78", "Вільний"),
            ("Бондаренко", "Сергій",    "+380 93 345 67 89", "Вільний"),
            ("Ткаченко",   "Віктор",    "+380 96 456 78 90", "Вільний"),
            ("Кравченко",  "Дмитро",    "+380 63 567 89 01", "Вільний"),
        };
        var driverIds = new List<long>();
        foreach (var d in drivers)
        {
            driverIds.Add(Insert(connection, transaction, """
                INSERT INTO Driver (last_name, first_name, phone, status)
                VALUES ($last, $first, $phone, $status)
                """,
                ("$last", d.Last), ("$first", d.First), ("$phone", d.Phone), ("$status", d.Status)));
        }

        // Типи вантажу
        string[] cargoTypes =
        {
            "Будівельні матеріали", "Продукти харчування", "Побутова техніка",
            "Меблі", "Промислове обладнання"
        };
        var typeIds = new List<long>();
        foreach (string name in cargoTypes)
        {
            typeIds.Add(Insert(connection, transaction,
                "INSERT INTO CargoType (name) VALUES ($name)", ("$name", name)));
        }

        // Вантажі
        var cargos = new (string Name, double Weight, int TypeIndex)[]
        {
            ("Цемент у мішках",     20000, 0),
            ("Молочна продукція",    8500, 1),
            ("Холодильники",         3200, 2),
            ("Офісні меблі",         4800, 3),
            ("Верстати",            12500, 4),
            ("Цегла",               18000, 0),
        };
        var cargoIds = new List<long>();
        foreach (var c in cargos)
        {
            cargoIds.Add(Insert(connection, transaction, """
                INSERT INTO Cargo (name, weight_kg, cargo_type_id)
                VALUES ($name, $weight, $type)
                """,
                ("$name", c.Name), ("$weight", c.Weight), ("$type", typeIds[c.TypeIndex])));
        }

        // Рейси: один завершений і один активний (його ресурси мають статус «В дорозі»)
        const string insertTrip = """
            INSERT INTO Trip (vehicle_id, driver_id, cargo_id, origin, destination,
                              start_date, end_date, status)
            VALUES ($vehicle, $driver, $cargo, $origin, $destination, $start, $end, $status)
            """;
        Insert(connection, transaction, insertTrip,
            ("$vehicle", vehicleIds[1]), ("$driver", driverIds[1]), ("$cargo", cargoIds[0]),
            ("$origin", "Київ"), ("$destination", "Суми"),
            ("$start", D(-10)), ("$end", D(-8)), ("$status", "Завершено"));
        Insert(connection, transaction, insertTrip,
            ("$vehicle", vehicleIds[0]), ("$driver", driverIds[0]), ("$cargo", cargoIds[1]),
            ("$origin", "Суми"), ("$destination", "Харків"),
            ("$start", D(-1)), ("$end", D(2)), ("$status", "В дорозі"));
    }
}
