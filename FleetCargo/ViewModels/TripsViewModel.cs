using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;
using FleetCargo.Services;

namespace FleetCargo.ViewModels;

/// <summary>Модуль «Рейси»: створення рейсу, вибір вільних ресурсів, завершення рейсу.</summary>
public class TripsViewModel : ViewModelBase
{
    private readonly TripService _tripService;
    private readonly AvailabilityService _availability;
    private readonly ICargoRepository _cargos;
    private readonly ITripRepository _trips;
    private readonly ExportService _exportService;
    private bool _isCreating;

    private DateTime? _startDate;
    private DateTime? _endDate;
    private Vehicle? _selectedVehicle;
    private Driver? _selectedDriver;
    private Cargo? _selectedCargo;
    private Trip? _selectedTrip;
    private string _origin = "";
    private string _destination = "";

    // Пошук рейсів
    private TripFilter? _activeFilter;
    private string _queryText = "";
    private bool _exactMatch;
    private string _statusFilter = "Усі";
    private DateTime? _filterFrom;
    private DateTime? _filterTo;
    private string _searchError = "";
    private string _resultText = "";

    public TripsViewModel(TripService tripService, AvailabilityService availability,
        ICargoRepository cargos, ITripRepository trips, ExportService exportService)
    {
        _exportService = exportService;
        _tripService = tripService;
        _availability = availability;
        _cargos = cargos;
        _trips = trips;

        FindAvailableCommand = new RelayCommand(FindAvailable);
        CreateTripCommand = new RelayCommand(CreateTrip, CanCreate);
        CompleteTripCommand = new RelayCommand(CompleteTrip,
            () => SelectedTrip != null && SelectedTrip.Status == TripStatus.InProgress);
        ExportCommand = new RelayCommand(Export, () => SelectedTrip != null);
        NewTripCommand = new RelayCommand(() =>
        {
            ClearForm();
            IsCreating = true;
        });
        CancelNewTripCommand = new RelayCommand(() => IsCreating = false);

        SearchCommand = new RelayCommand(Search);
        ResetSearchCommand = new RelayCommand(ResetSearch);
    }

    public event Action? DataChanged;

    public ObservableCollection<Trip> Trips { get; } = new();
    public ObservableCollection<Vehicle> FreeVehicles { get; } = new();
    public ObservableCollection<Driver> FreeDrivers { get; } = new();
    public ObservableCollection<Cargo> CargoList { get; } = new();
    public ObservableCollection<string> AvailableResources { get; } = new();

    public ICommand FindAvailableCommand { get; }
    public ICommand CreateTripCommand { get; }
    public ICommand CompleteTripCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand NewTripCommand { get; }
    public ICommand CancelNewTripCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetSearchCommand { get; }

    public IReadOnlyList<string> StatusOptions { get; } =
        new[] { "Усі", TripStatus.InProgress, TripStatus.Completed };

    /// <summary>Відкрито форму створення рейсу.</summary>
    public bool IsCreating { get => _isCreating; set => SetProperty(ref _isCreating, value); }

    public DateTime? StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
    public DateTime? EndDate { get => _endDate; set => SetProperty(ref _endDate, value); }
    public Vehicle? SelectedVehicle { get => _selectedVehicle; set => SetProperty(ref _selectedVehicle, value); }
    public Driver? SelectedDriver { get => _selectedDriver; set => SetProperty(ref _selectedDriver, value); }
    public Cargo? SelectedCargo { get => _selectedCargo; set => SetProperty(ref _selectedCargo, value); }
    public Trip? SelectedTrip { get => _selectedTrip; set => SetProperty(ref _selectedTrip, value); }
    public string Origin { get => _origin; set => SetProperty(ref _origin, value); }
    public string Destination { get => _destination; set => SetProperty(ref _destination, value); }

    public string QueryText { get => _queryText; set => SetProperty(ref _queryText, value); }

    public bool ExactMatch
    {
        get => _exactMatch;
        set
        {
            if (SetProperty(ref _exactMatch, value))
                OnPropertyChanged(nameof(PartialMatch));
        }
    }

    public bool PartialMatch
    {
        get => !_exactMatch;
        set
        {
            if (value)
                ExactMatch = false;
        }
    }

    public string StatusFilter { get => _statusFilter; set => SetProperty(ref _statusFilter, value); }
    public DateTime? FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    public DateTime? FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }
    public string SearchError { get => _searchError; set => SetProperty(ref _searchError, value); }
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    public void Load()
    {
        IReadOnlyList<Trip> shown = _activeFilter == null ? _trips.GetAll() : _trips.Search(_activeFilter);

        Trips.Clear();
        foreach (Trip t in shown)
            Trips.Add(t);

        CargoList.Clear();
        foreach (Cargo c in _cargos.GetAll())
            CargoList.Add(c);

        ResultText = _activeFilter == null ? ""
            : shown.Count == 0 ? "Нічого не знайдено." : $"Знайдено записів: {shown.Count}";
    }

    private void Search()
    {
        SearchError = "";
        if (FilterFrom != null && FilterTo != null && FilterFrom > FilterTo)
        {
            SearchError = "Початок періоду пізніший за його кінець.";
            return;
        }

        _activeFilter = new TripFilter
        {
            Text = QueryText,
            ExactMatch = ExactMatch,
            Status = StatusFilter == "Усі" ? null : StatusFilter,
            From = FilterFrom,
            To = FilterTo
        };
        Load();
    }

    private void ResetSearch()
    {
        QueryText = "";
        ExactMatch = false;
        StatusFilter = "Усі";
        FilterFrom = null;
        FilterTo = null;
        SearchError = "";
        _activeFilter = null;
        Load();
    }

    private bool CanCreate() =>
        SelectedVehicle != null && SelectedDriver != null && SelectedCargo != null &&
        !string.IsNullOrWhiteSpace(Origin) && !string.IsNullOrWhiteSpace(Destination) &&
        StartDate != null && EndDate != null;

    private void FindAvailable()
    {
        if (StartDate == null || EndDate == null)
        {
            Dialogs.Error("Вкажіть дати початку й завершення рейсу.");
            return;
        }

        AvailabilityResult result = _availability.GetAvailable(StartDate.Value, EndDate.Value);
        if (!result.Success)
        {
            Dialogs.Error(result.Error);
            return;
        }

        FreeVehicles.Clear();
        foreach (Vehicle v in result.Vehicles)
            FreeVehicles.Add(v);
        FreeDrivers.Clear();
        foreach (Driver d in result.Drivers)
            FreeDrivers.Add(d);

        AvailableResources.Clear();
        AvailableResources.Add("Вільні автомобілі: " + (result.Vehicles.Count == 0
            ? "немає" : string.Join(", ", result.Vehicles.Select(v => v.PlateNumber))));
        AvailableResources.Add("Вільні водії: " + (result.Drivers.Count == 0
            ? "немає" : string.Join(", ", result.Drivers.Select(d => d.FullName))));
    }

    private void CreateTrip()
    {
        OperationResult result = _tripService.CreateTrip(
            SelectedVehicle!.Id, SelectedDriver!.Id, SelectedCargo!.Id,
            Origin, Destination, StartDate!.Value, EndDate!.Value);

        if (!result.Success)
        {
            Dialogs.Error(result.Error);
            return;
        }

        ClearForm();
        IsCreating = false;
        Dialogs.Info($"Рейс № {result.Id} створено.");
        Load();
        DataChanged?.Invoke();
    }

    private void CompleteTrip()
    {
        if (SelectedTrip == null)
            return;
        if (!Dialogs.Confirm($"Завершити рейс № {SelectedTrip.Id}?"))
            return;

        OperationResult result = _tripService.CompleteTrip(SelectedTrip.Id);
        if (!result.Success)
        {
            Dialogs.Error(result.Error);
            return;
        }

        Load();
        DataChanged?.Invoke();
    }

    /// <summary>Зберігає маршрутний лист вибраного рейсу у файл Excel.</summary>
    private void Export()
    {
        if (SelectedTrip == null)
            return;

        string? path = Dialogs.PickSavePath($"Маршрутний_лист_{SelectedTrip.Id}");
        if (path == null)
            return;

        OperationResult result;
        try
        {
            result = _exportService.ExportRouteSheet(SelectedTrip.Id, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Dialogs.Error("Не вдалося зберегти файл: " + ex.Message);
            return;
        }

        if (!result.Success)
        {
            Dialogs.Error(result.Error);
            return;
        }

        Dialogs.OfferOpen(path);
    }

    private void ClearForm()
    {
        SelectedVehicle = null;
        SelectedDriver = null;
        SelectedCargo = null;
        FreeVehicles.Clear();
        FreeDrivers.Clear();
        AvailableResources.Clear();
        Origin = "";
        Destination = "";
        StartDate = null;
        EndDate = null;
    }
}
