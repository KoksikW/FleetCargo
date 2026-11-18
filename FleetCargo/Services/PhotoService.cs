using System.IO;
using System.Windows.Media.Imaging;
using FleetCargo.Data;

namespace FleetCargo.Services;

/// <summary>
/// Робота з фото: копіювання вибраного файлу в каталог програми, видалення та завантаження
/// зображень. У базі зберігається лише ім'я скопійованого файлу.
/// </summary>
public static class PhotoService
{
    /// <summary>Максимальний розмір файлу фото, байт (5 МБ).</summary>
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

    /// <summary>Каталог фото: %LocalAppData%\FleetCargo\Photos.</summary>
    public static string PhotosFolder => Path.Combine(DbSettings.DatabaseFolder, "Photos");

    /// <summary>Перевіряє формат і розмір файлу; повертає текст помилки або null.</summary>
    public static string? Validate(string sourcePath)
    {
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return "Підтримуються лише зображення JPG, PNG або BMP.";

        var info = new FileInfo(sourcePath);
        if (info.Exists && info.Length > MaxSizeBytes)
            return "Файл завеликий (максимум 5 МБ).";
        return null;
    }

    /// <summary>Копіює файл у каталог фото під унікальним іменем і повертає це ім'я.</summary>
    public static string Import(string sourcePath, string prefix)
    {
        string? error = Validate(sourcePath);
        if (error != null)
            throw new InvalidOperationException(error);

        Directory.CreateDirectory(PhotosFolder);
        string fileName = $"{prefix}_{Guid.NewGuid():N}{Path.GetExtension(sourcePath).ToLowerInvariant()}";
        File.Copy(sourcePath, Path.Combine(PhotosFolder, fileName));
        return fileName;
    }

    public static string? GetFullPath(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? null : Path.Combine(PhotosFolder, Path.GetFileName(fileName));

    /// <summary>Шукає в каталозі Photos файл із вказаним ім'ям будь-якого розширення; повертає повний шлях або null.</summary>
    public static string? FindFile(string nameWithoutExtension)
    {
        if (!Directory.Exists(PhotosFolder))
            return null;
        return Directory.GetFiles(PhotosFolder, nameWithoutExtension + ".*").FirstOrDefault();
    }

    /// <summary>Видаляє файл фото (помилки доступу ігноруються).</summary>
    public static void Delete(string? fileName)
    {
        string? path = GetFullPath(fileName);
        if (path == null || !File.Exists(path))
            return;
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Завантажує зображення з файлу без блокування файлу; null, якщо файлу немає або він пошкоджений.</summary>
    public static BitmapImage? LoadImage(string? fullPath)
    {
        if (fullPath == null || !File.Exists(fullPath))
            return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(fullPath);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Завантажує збережене фото за іменем файлу з каталогу Photos.</summary>
    public static BitmapImage? LoadStored(string? fileName) => LoadImage(GetFullPath(fileName));
}
