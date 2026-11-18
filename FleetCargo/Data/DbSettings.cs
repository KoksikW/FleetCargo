using System.IO;

namespace FleetCargo.Data;

/// <summary>Основні параметри підключення до бази даних.</summary>
public static class DbSettings
{
    /// <summary>Каталог бази: %LocalAppData%\FleetCargo.</summary>
    public static string DatabaseFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FleetCargo");

    /// <summary>Повний шлях до файлу бази даних.</summary>
    public static string DatabasePath { get; } = Path.Combine(DatabaseFolder, "fleetcargo.db");

    /// <summary>Контроль зовнішніх ключів (SQLite вимикає його за замовчуванням).</summary>
    public static bool ForeignKeys => true;

    /// <summary>Час очікування розблокування бази, секунд.</summary>
    public static int TimeoutSeconds => 30;
}
