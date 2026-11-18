using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;
using FleetCargo.Services;
using Microsoft.Data.Sqlite;

namespace FleetCargo.ViewModels;

/// <summary>Модуль «Автопарк»: список, форма додавання та редагування, видалення, попередження.</summary>
public class VehiclesViewModel : ViewModelBase
{
    private readonly IVehicleRepository _repository;
    private readonly AlertService _alertService;

    private Vehicle? _selectedVehicle;
    private Vehicle? _editing;          // null — додавання нового автомобіля
    private bool _isEditing;
    private string _formTitle = "";
    private string _plateNumber = "";
    private string _brand = "";
    private string _model = "";
    private string _mileageText = "0";
    private DateTime? _nextServiceDate;
    private DateTime? _insuranceEndDate;
    private string _errorText = "";

    // Пошук
    private readonly ICollectionView _view;
    private VehicleFilter? _activeFilter;
    private string _searchText = "";
    private string _queryText = "";
    private bool _exactMatch;
    private string _statusFilter = "Усі";
    private string _mileageFromText = "";
    private string _mileageToText = "";
    private DateTime? _serviceBeforeDate;
    private string _searchError = "";
    private string _resultText = "";

    // Фото та характеристика
    private string _notes = "";
    private string? _originalPhoto;        // фото запису до початку редагування
    private string? _pickedPhotoPath;      // новий вибраний файл (копіюється під час збереження)
    private bool _removePhoto;
    private ImageSource? _formPhoto;
    private ImageSource? _selectedPhoto;

    public VehiclesViewModel(IVehicleRepository repository, AlertService alertService)
    {
        _repository = repository;
        _alertService = alertService;

        AddCommand = new RelayCommand(() => StartEdit(null));
        EditCommand = new RelayCommand(() => StartEdit(SelectedVehicle), () => SelectedVehicle != null);
        DeleteCommand = new RelayCommand(Delete, () => SelectedVehicle != null);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => IsEditing = false);

        SearchCommand = new RelayCommand(Search);
        ResetSearchCommand = new RelayCommand(ResetSearch);
        PickPhotoCommand = new RelayCommand(PickPhoto);
        RemovePhotoCommand = new RelayCommand(RemovePhoto, () => FormPhoto != null);

        // Швидкий фільтр у таблиці працює над уже завантаженими записами, без запиту до бази
        _view = CollectionViewSource.GetDefaultView(Vehicles);
        _view.Filter = QuickFilter;
    }

    /// <summary>Виникає після змін у базі, щоб інші модулі оновили свої дані.</summary>
    public event Action? DataChanged;

    public ObservableCollection<Vehicle> Vehicles { get; } = new();
    public ObservableCollection<string> Alerts { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetSearchCommand { get; }
    public ICommand PickPhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }

    public IReadOnlyList<string> StatusOptions { get; } =
        new[] { "Усі", ResourceStatus.Free, ResourceStatus.OnTrip };

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (SetProperty(ref _selectedVehicle, value))
            {
                SelectedPhoto = PhotoService.LoadStored(value?.PhotoPath);
                OnPropertyChanged(nameof(IsDetailsVisible));
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
                OnPropertyChanged(nameof(IsDetailsVisible));
        }
    }

    /// <summary>Картка вибраного автомобіля видима, коли форма редагування закрита.</summary>
    public bool IsDetailsVisible => !IsEditing && SelectedVehicle != null;

    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public ImageSource? FormPhoto
    {
        get => _formPhoto;
        set
        {
            if (SetProperty(ref _formPhoto, value))
                OnPropertyChanged(nameof(NoFormPhoto));
        }
    }

    public ImageSource? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            if (SetProperty(ref _selectedPhoto, value))
                OnPropertyChanged(nameof(NoSelectedPhoto));
        }
    }

    public bool NoFormPhoto => _formPhoto == null;
    public bool NoSelectedPhoto => _selectedPhoto == null;
    public string FormTitle { get => _formTitle; set => SetProperty(ref _formTitle, value); }
    public string PlateNumber { get => _plateNumber; set => SetProperty(ref _plateNumber, value); }
    public string Brand { get => _brand; set => SetProperty(ref _brand, value); }
    public string Model { get => _model; set => SetProperty(ref _model, value); }
    public string MileageText { get => _mileageText; set => SetProperty(ref _mileageText, value); }
    public DateTime? NextServiceDate { get => _nextServiceDate; set => SetProperty(ref _nextServiceDate, value); }
    public DateTime? InsuranceEndDate { get => _insuranceEndDate; set => SetProperty(ref _insuranceEndDate, value); }
    public string ErrorText { get => _errorText; set => SetProperty(ref _errorText, value); }

    /// <summary>Швидкий пошук у вже показаних записах.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _view.Refresh();
        }
    }

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
    public string MileageFromText { get => _mileageFromText; set => SetProperty(ref _mileageFromText, value); }
    public string MileageToText { get => _mileageToText; set => SetProperty(ref _mileageToText, value); }
    public DateTime? ServiceBeforeDate { get => _serviceBeforeDate; set => SetProperty(ref _serviceBeforeDate, value); }
    public string SearchError { get => _searchError; set => SetProperty(ref _searchError, value); }
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    /// <summary>
    /// Зчитує автомобілі з бази (усі або за активними критеріями пошуку),
    /// обчислює попередження та оновлює списки.
    /// </summary>
    public void Load()
    {
        IReadOnlyList<Vehicle> all = _repository.GetAll();
        IReadOnlyList<Vehicle> shown = _activeFilter == null ? all : _repository.Search(_activeFilter);
        DateTime today = DateTime.Today;
        _alertService.Apply(shown, today);

        Vehicles.Clear();
        foreach (Vehicle v in shown)
            Vehicles.Add(v);

        // Попередження формуються за всім автопарком, незалежно від пошуку
        Alerts.Clear();
        foreach (AlertItem alert in _alertService.BuildAlerts(all, today))
            Alerts.Add(alert.Text);
        if (Alerts.Count == 0)
            Alerts.Add("Попереджень немає");

        ResultText = _activeFilter == null ? ""
            : shown.Count == 0 ? "Нічого не знайдено." : $"Знайдено записів: {shown.Count}";
    }

    private bool QuickFilter(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText) || item is not Vehicle v)
            return true;
        string text = SearchText.Trim().ToLowerInvariant();
        return v.PlateNumber.ToLowerInvariant().Contains(text)
            || v.Brand.ToLowerInvariant().Contains(text)
            || v.Model.ToLowerInvariant().Contains(text);
    }

    private void Search()
    {
        SearchError = "";
        if (!TryParseNonNegative(MileageFromText, out int? mileageFrom) ||
            !TryParseNonNegative(MileageToText, out int? mileageTo))
        {
            SearchError = "Пробіг має бути цілим невід’ємним числом.";
            return;
        }
        if (mileageFrom != null && mileageTo != null && mileageFrom > mileageTo)
        {
            SearchError = "Початкове значення пробігу більше за кінцеве.";
            return;
        }

        _activeFilter = new VehicleFilter
        {
            Text = QueryText,
            ExactMatch = ExactMatch,
            Status = StatusFilter == "Усі" ? null : StatusFilter,
            MileageFrom = mileageFrom,
            MileageTo = mileageTo,
            ServiceBefore = ServiceBeforeDate
        };
        Load();
    }

    private void ResetSearch()
    {
        QueryText = "";
        ExactMatch = false;
        StatusFilter = "Усі";
        MileageFromText = "";
        MileageToText = "";
        ServiceBeforeDate = null;
        SearchError = "";
        SearchText = "";
        _activeFilter = null;
        Load();
    }

    private void StartEdit(Vehicle? vehicle)
    {
        _editing = vehicle;
        FormTitle = vehicle == null ? "Новий автомобіль" : "Редагування автомобіля";
        PlateNumber = vehicle?.PlateNumber ?? "";
        Brand = vehicle?.Brand ?? "";
        Model = vehicle?.Model ?? "";
        MileageText = (vehicle?.Mileage ?? 0).ToString();
        NextServiceDate = vehicle?.NextServiceDate;
        InsuranceEndDate = vehicle?.InsuranceEndDate;
        Notes = vehicle?.Notes ?? "";
        _originalPhoto = vehicle?.PhotoPath;
        _pickedPhotoPath = null;
        _removePhoto = false;
        FormPhoto = PhotoService.LoadStored(vehicle?.PhotoPath);
        ErrorText = "";
        IsEditing = true;
    }

    private string? Validate(out int mileage)
    {
        mileage = 0;
        if (string.IsNullOrWhiteSpace(PlateNumber)) return "Вкажіть державний номер.";
        if (PlateNumber.Trim().Length > 15) return "Державний номер задовгий (до 15 символів).";
        if (string.IsNullOrWhiteSpace(Brand)) return "Вкажіть марку.";
        if (string.IsNullOrWhiteSpace(Model)) return "Вкажіть модель.";
        if (!int.TryParse(MileageText, out mileage) || mileage < 0)
            return "Пробіг має бути цілим невід’ємним числом.";
        if (NextServiceDate == null) return "Вкажіть дату наступного ТО.";
        if (InsuranceEndDate == null) return "Вкажіть дату закінчення страхування.";
        if (Notes.Length > 1000) return "Примітки задовгі (до 1000 символів).";
        return null;
    }

    private void Save()
    {
        string? error = Validate(out int mileage);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        // Фото: нове вибране копіюється в каталог програми, старе видаляється після збереження
        string? newPhoto = _removePhoto ? null : _originalPhoto;
        if (_pickedPhotoPath != null)
        {
            try
            {
                newPhoto = PhotoService.Import(_pickedPhotoPath, "vehicle");
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                ErrorText = ex.Message;
                return;
            }
        }

        Vehicle vehicle = _editing ?? new Vehicle();
        vehicle.PlateNumber = PlateNumber.Trim();
        vehicle.Brand = Brand.Trim();
        vehicle.Model = Model.Trim();
        vehicle.Mileage = mileage;
        vehicle.NextServiceDate = NextServiceDate!.Value;
        vehicle.InsuranceEndDate = InsuranceEndDate!.Value;
        vehicle.PhotoPath = newPhoto;
        vehicle.Notes = Notes.Trim();

        try
        {
            if (_editing == null)
                _repository.Add(vehicle);
            else
                _repository.Update(vehicle);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            if (newPhoto != _originalPhoto)
                PhotoService.Delete(newPhoto);
            ErrorText = "Автомобіль з таким номером уже існує.";
            Load();
            return;
        }

        if (_originalPhoto != null && _originalPhoto != newPhoto)
            PhotoService.Delete(_originalPhoto);

        IsEditing = false;
        Load();
        DataChanged?.Invoke();
    }

    private void PickPhoto()
    {
        string? path = Dialogs.PickImage();
        if (path == null)
            return;

        string? error = PhotoService.Validate(path);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        ImageSource? image = PhotoService.LoadImage(path);
        if (image == null)
        {
            ErrorText = "Не вдалося відкрити зображення.";
            return;
        }

        _pickedPhotoPath = path;
        _removePhoto = false;
        FormPhoto = image;
        ErrorText = "";
    }

    private void RemovePhoto()
    {
        _pickedPhotoPath = null;
        _removePhoto = true;
        FormPhoto = null;
    }

    private void Delete()
    {
        if (SelectedVehicle == null)
            return;
        if (!Dialogs.Confirm($"Видалити автомобіль {SelectedVehicle.PlateNumber}?"))
            return;

        string? photo = SelectedVehicle.PhotoPath;
        try
        {
            _repository.Delete(SelectedVehicle.Id);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Dialogs.Error("Автомобіль не можна видалити: з ним пов’язані рейси.");
            return;
        }

        PhotoService.Delete(photo);
        Load();
        DataChanged?.Invoke();
    }
}
