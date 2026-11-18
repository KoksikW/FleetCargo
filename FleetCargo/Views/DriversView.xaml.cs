using System.Windows.Controls;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

public partial class DriversView : UserControl
{
    private readonly EditDialogController<DriversViewModel> _dialog;

    public DriversView()
    {
        InitializeComponent();

        // Форма додавання та редагування водія відкривається в окремому вікні
        _dialog = new EditDialogController<DriversViewModel>(this,
            nameof(DriversViewModel.IsEditing),
            vm => vm.IsEditing, vm => vm.FormTitle,
            () => new DriverFormView(), vm => vm.CancelCommand.Execute(null));
    }
}
