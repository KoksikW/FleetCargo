using System.Windows;
using System.Windows.Controls;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

public partial class TripsView : UserControl
{
    private readonly EditDialogController<TripsViewModel> _dialog;

    public TripsView()
    {
        InitializeComponent();

        // Форма створення рейсу відкривається в окремому вікні
        _dialog = new EditDialogController<TripsViewModel>(this,
            nameof(TripsViewModel.IsCreating),
            vm => vm.IsCreating, _ => "Новий рейс",
            () => new TripFormView(), vm => vm.CancelNewTripCommand.Execute(null));
    }

    /// <summary>Відкриває вікно «Вантажі»; після закриття оновлює список вантажів для нового рейсу.</summary>
    private void OnCargoClick(object sender, RoutedEventArgs e)
    {
        var window = new CargoWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();

        if (DataContext is TripsViewModel vm)
            vm.Load();
    }
}
