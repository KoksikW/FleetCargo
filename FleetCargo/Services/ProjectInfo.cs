namespace FleetCargo.Services;

/// <summary>Відомості про проєкт та автора для вкладки «Авторство».</summary>
public static class ProjectInfo
{
    public const string AuthorName = "Беседа Матвєй";
    public const string Group = "731";
    public const string Title = "Fleet & Cargo — логістична система управління автопарком та маршрутами";
    /// <summary>Ім'я файлу фото автора (без розширення) у каталозі Photos.</summary>
    public const string AuthorPhotoName = "author_6fdb09853ef9461aa104fca1fc2d2f6e";

    public const string Technologies = "C#, WPF (.NET 10), SQLite, ClosedXML";

    /// <summary>Дата початку розробки.</summary>
    public static readonly DateTime StartDate = new(2026, 10, 12);

    /// <summary>Дата завершення розробки; null, доки проєкт не завершено.</summary>
    public static readonly DateTime? EndDate = null;
}
