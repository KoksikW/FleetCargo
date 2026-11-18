using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;
using FleetCargo.Services;
using Microsoft.Data.Sqlite;

namespace FleetCargo.ViewModels;

/// <summary>Вікно «Вантажі»: список вантажів, додавання, редагування, видалення та типи вантажу.</summary>
public class CargoViewModel : ViewModelBase
{
    private readonly ICargoRepository _repository;

    private Cargo? _selectedCargo;
    private string _name = "";
    private string _weightText = "";
    private CargoType? _selectedType;
    private string _newTypeName = "";
    private string _errorText = "";

    public CargoViewModel(ICargoRepository repository)
    {
        _repository = repository;

        AddCommand = new RelayCommand(Add);
        UpdateCommand = new RelayCommand(Update, () => SelectedCargo != null);
        DeleteCommand = new RelayCommand(Delete, () => SelectedCargo != null);
        ClearCommand = new RelayCommand(ClearForm);
        AddTypeCommand = new RelayCommand(AddType);

        Load();
    }

    public ObservableCollection<Cargo> Cargos { get; } = new();
    public ObservableCollection<CargoType> Types { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand AddTypeCommand { get; }

    public Cargo? SelectedCargo
    {
        get => _selectedCargo;
        set
        {
            if (!SetProperty(ref _selectedCargo, value) || value == null)
                return;
            Name = value.Name;
            WeightText = value.WeightKg.ToString("0.##", CultureInfo.CurrentCulture);
            SelectedType = Types.FirstOrDefault(t => t.Id == value.CargoTypeId);
            ErrorText = "";
        }
    }

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string WeightText { get => _weightText; set => SetProperty(ref _weightText, value); }
    public CargoType? SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }
    public string NewTypeName { get => _newTypeName; set => SetProperty(ref _newTypeName, value); }
    public string ErrorText { get => _errorText; set => SetProperty(ref _errorText, value); }

    private void Load()
    {
        Types.Clear();
        foreach (CargoType t in _repository.GetTypes())
            Types.Add(t);

        Cargos.Clear();
        foreach (Cargo c in _repository.GetAll())
            Cargos.Add(c);
    }

    private string? Validate(out double weight)
    {
        weight = 0;
        if (string.IsNullOrWhiteSpace(Name)) return "Вкажіть назву вантажу.";
        if (!double.TryParse(WeightText.Trim().Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out weight) || weight <= 0)
            return "Вага має бути додатним числом.";
        if (SelectedType == null) return "Виберіть тип вантажу.";
        return null;
    }

    private void Add()
    {
        string? error = Validate(out double weight);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        _repository.Add(new Cargo { Name = Name.Trim(), WeightKg = weight, CargoTypeId = SelectedType!.Id });
        ClearForm();
        Load();
    }

    private void Update()
    {
        if (SelectedCargo == null)
            return;
        string? error = Validate(out double weight);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        var cargo = new Cargo
        {
            Id = SelectedCargo.Id,
            Name = Name.Trim(),
            WeightKg = weight,
            CargoTypeId = SelectedType!.Id
        };
        _repository.Update(cargo);
        ClearForm();
        Load();
    }

    private void Delete()
    {
        if (SelectedCargo == null)
            return;
        if (!Dialogs.Confirm($"Видалити вантаж «{SelectedCargo.Name}»?"))
            return;

        try
        {
            _repository.Delete(SelectedCargo.Id);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Dialogs.Error("Вантаж не можна видалити: він використовується в рейсах.");
            return;
        }

        ClearForm();
        Load();
    }

    private void AddType()
    {
        if (string.IsNullOrWhiteSpace(NewTypeName))
        {
            ErrorText = "Вкажіть назву нового типу вантажу.";
            return;
        }

        try
        {
            _repository.AddType(new CargoType { Name = NewTypeName.Trim() });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            ErrorText = "Такий тип вантажу вже існує.";
            return;
        }

        NewTypeName = "";
        ErrorText = "";
        Load();
    }

    private void ClearForm()
    {
        SelectedCargo = null;
        Name = "";
        WeightText = "";
        SelectedType = null;
        ErrorText = "";
    }
}
