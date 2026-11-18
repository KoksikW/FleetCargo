using System.Windows;
using System.Windows.Markup;
using FleetCargo.Data;
using FleetCargo.Services;

namespace FleetCargo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Мова за замовчуванням для всіх елементів: прив'язки дат розбирають значення формату дд.мм.рррр
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("uk-UA")));

        try
        {
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося ініціалізувати базу даних:\n{ex.Message}",
                "Fleet & Cargo", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Налаштування інтерфейсу з минулого сеансу (тема застосовується до створення вікон)
        SettingsService.Load();
        ThemeService.Apply(SettingsService.Current.AccentColor);

        base.OnStartup(e);
    }
}
