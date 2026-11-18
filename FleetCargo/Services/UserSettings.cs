namespace FleetCargo.Services;

/// <summary>
/// Особисті налаштування інтерфейсу користувача. Зберігаються у файлі XML між запусками.
/// Нульові розміри вікна означають, що значення ще не збережено.
/// </summary>
public class UserSettings
{
    public double WindowLeft { get; set; }
    public double WindowTop { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }

    /// <summary>Normal або Maximized.</summary>
    public string WindowState { get; set; } = "Maximized";

    /// <summary>Номер вибраної вкладки (0 – «Автопарк»).</summary>
    public int SelectedTab { get; set; }

    /// <summary>Тип останнього звіту та період, вибраний у модулі «Звіти».</summary>
    public string ReportType { get; set; } = "";
    public DateTime? ReportPeriodStart { get; set; }
    public DateTime? ReportPeriodEnd { get; set; }

    /// <summary>Акцентний колір інтерфейсу у форматі #RRGGBB.</summary>
    public string AccentColor { get; set; } = ThemeService.DefaultAccent;

}
