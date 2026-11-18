using System.IO;
using System.Xml;
using System.Xml.Serialization;
using FleetCargo.Data;

namespace FleetCargo.Services;

/// <summary>Читання та збереження налаштувань інтерфейсу у файлі XML.</summary>
public static class SettingsService
{
    /// <summary>Файл налаштувань: %LocalAppData%\FleetCargo\settings.xml.</summary>
    public static string FilePath => Path.Combine(DbSettings.DatabaseFolder, "settings.xml");

    /// <summary>Поточні налаштування, завантажені під час запуску програми.</summary>
    public static UserSettings Current { get; private set; } = new();

    /// <summary>Завантажує налаштування; якщо файлу немає або він пошкоджений – значення за замовчуванням.</summary>
    public static void Load() => Current = Read(FilePath);

    /// <summary>Зберігає поточні налаштування (помилка запису не переривають роботу програми, бо вона не критична).</summary>
    public static void Save() => Write(Current, FilePath);

    public static UserSettings Read(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new UserSettings();

            using FileStream stream = File.OpenRead(path);
            var serializer = new XmlSerializer(typeof(UserSettings));
            return serializer.Deserialize(stream) as UserSettings ?? new UserSettings();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException
                                       or UnauthorizedAccessException or XmlException)
        {
            return new UserSettings();
        }
    }

    public static bool Write(UserSettings settings, string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp = path + ".tmp";
            using (FileStream stream = File.Create(temp))
                new XmlSerializer(typeof(UserSettings)).Serialize(stream, settings);
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException
                                       or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
