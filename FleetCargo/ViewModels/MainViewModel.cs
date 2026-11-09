namespace FleetCargo.ViewModels;

/// <summary>Модель подання головного вікна: вибрана вкладка та рядок стану.</summary>
public class MainViewModel : ViewModelBase
{
    private static readonly string[] TabNames = { "Автопарк", "Водії", "Рейси", "Звіти" };

    private int _selectedTabIndex;
    private string _statusText = "Готово до роботи";
    private string _recordsText = "";

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value) &&
                value >= 0 && value < TabNames.Length)
            {
                StatusText = $"Відкрито вкладку «{TabNames[value]}»";
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string RecordsText
    {
        get => _recordsText;
        set => SetProperty(ref _recordsText, value);
    }

    public string TodayText => DateTime.Today.ToString("dd.MM.yyyy");
}
