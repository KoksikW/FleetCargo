namespace FleetCargo.Data;

/// <summary>SQL-скрипти створення структури бази даних SQLite.</summary>
public static class DatabaseSchema
{
    /// <summary>Таблиці: Vehicle, Driver, CargoType, Cargo, Trip.</summary>
    public const string CreateTables = """
        CREATE TABLE IF NOT EXISTS Vehicle (
            vehicle_id          INTEGER PRIMARY KEY AUTOINCREMENT,
            plate_number        TEXT    NOT NULL UNIQUE,
            brand               TEXT    NOT NULL,
            model               TEXT    NOT NULL,
            mileage_km          INTEGER NOT NULL DEFAULT 0 CHECK (mileage_km >= 0),
            next_service_date   TEXT    NOT NULL,
            insurance_end_date  TEXT    NOT NULL,
            status              TEXT    NOT NULL DEFAULT 'Вільний'
                                CHECK (status IN ('Вільний', 'В дорозі'))
        );

        CREATE TABLE IF NOT EXISTS Driver (
            driver_id   INTEGER PRIMARY KEY AUTOINCREMENT,
            last_name   TEXT NOT NULL,
            first_name  TEXT NOT NULL,
            phone       TEXT NOT NULL DEFAULT '',
            status      TEXT NOT NULL DEFAULT 'Вільний'
                        CHECK (status IN ('Вільний', 'В дорозі'))
        );

        CREATE TABLE IF NOT EXISTS CargoType (
            cargo_type_id  INTEGER PRIMARY KEY AUTOINCREMENT,
            name           TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS Cargo (
            cargo_id       INTEGER PRIMARY KEY AUTOINCREMENT,
            name           TEXT NOT NULL,
            weight_kg      REAL NOT NULL CHECK (weight_kg > 0),
            cargo_type_id  INTEGER NOT NULL
                           REFERENCES CargoType (cargo_type_id) ON DELETE RESTRICT
        );

        CREATE TABLE IF NOT EXISTS Trip (
            trip_id      INTEGER PRIMARY KEY AUTOINCREMENT,
            vehicle_id   INTEGER NOT NULL
                         REFERENCES Vehicle (vehicle_id) ON DELETE RESTRICT,
            driver_id    INTEGER NOT NULL
                         REFERENCES Driver (driver_id) ON DELETE RESTRICT,
            cargo_id     INTEGER NOT NULL
                         REFERENCES Cargo (cargo_id) ON DELETE RESTRICT,
            origin       TEXT NOT NULL,
            destination  TEXT NOT NULL,
            start_date   TEXT NOT NULL,
            end_date     TEXT NOT NULL,
            status       TEXT NOT NULL DEFAULT 'В дорозі'
                         CHECK (status IN ('В дорозі', 'Завершено')),
            CHECK (end_date >= start_date)
        );
        """;

    /// <summary>Додаткові поля (фото та характеристика), що додаються до наявної бази.</summary>
    public static readonly (string Table, string Column, string Definition)[] Migrations =
    {
        ("Vehicle", "photo_path", "TEXT NULL"),
        ("Vehicle", "notes",      "TEXT NOT NULL DEFAULT ''"),
        ("Driver",  "photo_path", "TEXT NULL"),
        ("Driver",  "notes",      "TEXT NOT NULL DEFAULT ''"),
    };

    /// <summary>Індекси для швидкої вибірки доступності ресурсів і зв'язків.</summary>
    public const string CreateIndexes = """
        CREATE INDEX IF NOT EXISTS IX_Trip_Vehicle_Dates ON Trip (vehicle_id, start_date, end_date);
        CREATE INDEX IF NOT EXISTS IX_Trip_Driver_Dates  ON Trip (driver_id, start_date, end_date);
        CREATE INDEX IF NOT EXISTS IX_Trip_Cargo         ON Trip (cargo_id);
        CREATE INDEX IF NOT EXISTS IX_Cargo_Type         ON Cargo (cargo_type_id);
        """;
}
