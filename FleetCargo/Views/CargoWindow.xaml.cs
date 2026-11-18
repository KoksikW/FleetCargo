using System.Windows;
using FleetCargo.Data;
using FleetCargo.Data.Repositories;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

/// <summary>Окреме вікно керування вантажами та типами вантажу.</summary>
public partial class CargoWindow : Window
{
    public CargoWindow()
    {
        InitializeComponent();
        DataContext = new CargoViewModel(new CargoRepository(new SqliteConnectionFactory()));
    }
}
