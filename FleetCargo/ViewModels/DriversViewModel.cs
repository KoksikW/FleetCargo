using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;
using FleetCargo.Services;
using Microsoft.Data.Sqlite;

namespace FleetCargo.ViewModels;

/// <summary>Модуль «Водії»: список, форма додавання та редагування, видалення.</summary>
public class DriversViewModel : ViewModelBase
{
    private static readonly Regex PhonePattern = new(@"^\+?[0-9\s\-()]{7,20}$");

    private readonly IDriverRepository _repository;

    private Driver? _selectedDriver;
    private Driver? _editing;
    private bool _isEditing;
    private string _formTitle = "";
    private string _lastName = "";
    private string _firstName = "";
    private string _phone = "";
    private string _errorText = "";

    // Пошук
    private readonly ICollectionView _view;
    private DriverFilter? _activeFilter;
    private string _searchText = "";
    private string _queryText = "";
    private bool _exactMatch;
    private string _statusFilter = "Усі";
    private string _resultText = "";

    // Фото та характеристика
    private string _notes = "";
    private string? _originalPhoto;
    private string? _pickedPhotoPath;
    private bool _removePhoto;
    private ImageSource? _formPhoto;
    private ImageSource? _selectedPhoto;

    public DriversViewModel(IDriverRepository repository)
    {
        _repository = repository;

        AddCommand = new RelayCommand(() => StartEdit(null));
        EditCommand = new RelayCommand(() => StartEdit(SelectedDriver), () => SelectedDriver != null);
        DeleteCommand = new RelayCommand(Delete, () => SelectedDriver != null);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => IsEditing = false);

        SearchCommand = new RelayCommand(Search);
        ResetSearchCommand = new RelayCommand(ResetSearch);
        PickPhotoCommand = new RelayCommand(PickPhoto);
        RemovePhotoCommand = new RelayCommand(RemovePhoto, () => FormPhoto != null);

        _view = CollectionViewSource.GetDefaultView(Drivers);
        _view.Filter = QuickFilter;
    }

    public event Action? DataChanged;

    public ObservableCollection<Driver> Drivers { get; } = new();

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

    public Driver? SelectedDriver
    {
        get => _selectedDriver;
        set
        {
            if (SetProperty(ref _selectedDriver, value))
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

    public bool IsDetailsVisible => !IsEditing && SelectedDriver != null;

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
    public string LastName { get => _lastName; set => SetProperty(ref _lastName, value); }
    public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
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
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    public void Load()
    {
        IReadOnlyList<Driver> shown = _activeFilter == null ? _repository.GetAll() : _repository.Search(_activeFilter);

        Drivers.Clear();
        foreach (Driver d in shown)
            Drivers.Add(d);

        ResultText = _activeFilter == null ? ""
            : shown.Count == 0 ? "Нічого не знайдено." : $"Знайдено записів: {shown.Count}";
    }

    private bool QuickFilter(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText) || item is not Driver d)
            return true;
        string text = SearchText.Trim().ToLowerInvariant();
        return d.LastName.ToLowerInvariant().Contains(text)
            || d.FirstName.ToLowerInvariant().Contains(text)
            || d.Phone.ToLowerInvariant().Contains(text);
    }

    private void Search()
    {
        _activeFilter = new DriverFilter
        {
            Text = QueryText,
            ExactMatch = ExactMatch,
            Status = StatusFilter == "Усі" ? null : StatusFilter
        };
        Load();
    }

    private void ResetSearch()
    {
        QueryText = "";
        ExactMatch = false;
        StatusFilter = "Усі";
        SearchText = "";
        _activeFilter = null;
        Load();
    }

    private void StartEdit(Driver? driver)
    {
        _editing = driver;
        FormTitle = driver == null ? "Новий водій" : "Редагування водія";
        LastName = driver?.LastName ?? "";
        FirstName = driver?.FirstName ?? "";
        Phone = driver?.Phone ?? "";
        Notes = driver?.Notes ?? "";
        _originalPhoto = driver?.PhotoPath;
        _pickedPhotoPath = null;
        _removePhoto = false;
        FormPhoto = PhotoService.LoadStored(driver?.PhotoPath);
        ErrorText = "";
        IsEditing = true;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(LastName)) return "Вкажіть прізвище.";
        if (string.IsNullOrWhiteSpace(FirstName)) return "Вкажіть ім’я.";
        if (!string.IsNullOrWhiteSpace(Phone) && !PhonePattern.IsMatch(Phone.Trim()))
            return "Телефон має містити 7–20 символів: цифри, пробіли, «+», «-», дужки.";
        if (Notes.Length > 1000) return "Характеристика задовга (до 1000 символів).";
        return null;
    }

    private void Save()
    {
        string? error = Validate();
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        string? newPhoto = _removePhoto ? null : _originalPhoto;
        if (_pickedPhotoPath != null)
        {
            try
            {
                newPhoto = PhotoService.Import(_pickedPhotoPath, "driver");
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                ErrorText = ex.Message;
                return;
            }
        }

        Driver driver = _editing ?? new Driver();
        driver.LastName = LastName.Trim();
        driver.FirstName = FirstName.Trim();
        driver.Phone = Phone.Trim();
        driver.PhotoPath = newPhoto;
        driver.Notes = Notes.Trim();

        if (_editing == null)
            _repository.Add(driver);
        else
            _repository.Update(driver);

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
        if (SelectedDriver == null)
            return;
        if (!Dialogs.Confirm($"Видалити водія {SelectedDriver.FullName}?"))
            return;

        string? photo = SelectedDriver.PhotoPath;
        try
        {
            _repository.Delete(SelectedDriver.Id);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Dialogs.Error("Водія не можна видалити: з ним пов’язані рейси.");
            return;
        }

        PhotoService.Delete(photo);
        Load();
        DataChanged?.Invoke();
    }
}
