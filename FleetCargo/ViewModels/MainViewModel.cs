using FleetCargo.Data;
using FleetCargo.Data.Repositories;
using FleetCargo.Services;

namespace FleetCargo.ViewModels;

/// <summary>Модель подання головного вікна: модулі, вибрана вкладка та рядок стану.</summary>
public class MainViewModel : ViewModelBase
{
    /// <summary>Номер вкладки «Авторство»: вона відкриває вікно й ніколи не лишається вибраною.</summary>
    public const int AuthorTabIndex = 4;

    private static readonly string[] TabNames = { "Автопарк", "Водії", "Рейси", "Звіти", "Авторство" };

    private int _selectedTabIndex;
    private string _statusText = "Готово до роботи";
    private string _recordsText = "";
    private string _accentColor = ThemeService.DefaultAccent;

    public MainViewModel()
    {
        try
        {
            var factory = new SqliteConnectionFactory();
            var vehicles = new VehicleRepository(factory);
            var drivers = new DriverRepository(factory);
            var cargos = new CargoRepository(factory);
            var trips = new TripRepository(factory);

            var alertService = new AlertService();
            var exportService = new ExportService(vehicles, drivers, cargos, trips);

            VehiclesModule = new VehiclesViewModel(vehicles, alertService);
            DriversModule = new DriversViewModel(drivers);
            TripsModule = new TripsViewModel(
                new TripService(factory, vehicles, drivers, trips),
                new AvailabilityService(vehicles, drivers), cargos, trips, exportService);
            ReportsModule = new ReportsViewModel(
                new ReportService(trips, vehicles, alertService), exportService);

            // Зміни в одному модулі оновлюють дані в усіх (наприклад, статуси після створення рейсу)
            VehiclesModule.DataChanged += ReloadAll;
            DriversModule.DataChanged += ReloadAll;
            TripsModule.DataChanged += ReloadAll;

            AuthorModule = new AuthorViewModel();

            ReloadAll();
            RestoreSettings(SettingsService.Current);
        }
        catch (Exception ex)
        {
            StatusText = "Помилка читання бази даних: " + ex.Message;
        }
    }

    public VehiclesViewModel? VehiclesModule { get; }
    public DriversViewModel? DriversModule { get; }
    public TripsViewModel? TripsModule { get; }
    public ReportsViewModel? ReportsModule { get; }
    public AuthorViewModel? AuthorModule { get; }

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

    /// <summary>Акцентний колір інтерфейсу (#RRGGBB); зміна одразу застосовується до вікон.</summary>
    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (SetProperty(ref _accentColor, value))
                ThemeService.Apply(value);
        }
    }

    public string TodayText => DateTime.Today.ToString("dd.MM.yyyy");

    /// <summary>Перечитує дані всіх модулів і оновлює лічильники в рядку стану.</summary>
    private void ReloadAll()
    {
        VehiclesModule!.Load();
        DriversModule!.Load();
        TripsModule!.Load();
        RecordsText = $"Автомобілів: {VehiclesModule.Vehicles.Count}  |  " +
                      $"Водіїв: {DriversModule.Drivers.Count}  |  " +
                      $"Рейсів: {TripsModule.Trips.Count}";
    }

    /// <summary>Відновлює збережені налаштування: тему, вкладку та параметри звіту.</summary>
    private void RestoreSettings(UserSettings settings)
    {
        AccentColor = ThemeService.TryParse(settings.AccentColor, out _) ? settings.AccentColor : ThemeService.DefaultAccent;

        if (settings.SelectedTab >= 0 && settings.SelectedTab < AuthorTabIndex)
            SelectedTabIndex = settings.SelectedTab;

        if (ReportsModule != null)
        {
            ReportsModule.PeriodStart = settings.ReportPeriodStart;
            ReportsModule.PeriodEnd = settings.ReportPeriodEnd;
            if (ReportService.ReportNames.Contains(settings.ReportType))
                ReportsModule.SelectedReportType = settings.ReportType;
        }
    }

    /// <summary>Записує поточний стан інтерфейсу в об'єкт налаштувань (під час закриття вікна).</summary>
    public void CollectSettings(UserSettings settings)
    {
        settings.AccentColor = AccentColor;
        settings.SelectedTab = SelectedTabIndex;
        if (ReportsModule != null)
        {
            settings.ReportType = ReportsModule.SelectedReportType;
            settings.ReportPeriodStart = ReportsModule.PeriodStart;
            settings.ReportPeriodEnd = ReportsModule.PeriodEnd;
        }
    }
}
