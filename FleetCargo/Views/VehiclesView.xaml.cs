using System.Windows.Controls;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

public partial class VehiclesView : UserControl
{
    private readonly EditDialogController<VehiclesViewModel> _dialog;

    public VehiclesView()
    {
        InitializeComponent();

        // Форма додавання та редагування автомобіля відкривається в окремому вікні
        _dialog = new EditDialogController<VehiclesViewModel>(this,
            nameof(VehiclesViewModel.IsEditing),
            vm => vm.IsEditing, vm => vm.FormTitle,
            () => new VehicleFormView(), vm => vm.CancelCommand.Execute(null));
    }
}
