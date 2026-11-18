using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace FleetCargo.Views;

/// <summary>
/// Відкриває форму у модальному вікні, коли відповідна ознака моделі подання
/// (наприклад, IsEditing) стає true, і закриває її, коли ознака скидається.
/// </summary>
public sealed class EditDialogController<TViewModel> where TViewModel : class, INotifyPropertyChanged
{
    private readonly FrameworkElement _owner;
    private readonly string _flagName;
    private readonly Func<TViewModel, bool> _isOpen;
    private readonly Func<TViewModel, string> _title;
    private readonly Func<UserControl> _createForm;
    private readonly Action<TViewModel> _cancel;

    private TViewModel? _viewModel;
    private FormWindow? _window;

    public EditDialogController(FrameworkElement owner, string flagName,
        Func<TViewModel, bool> isOpen, Func<TViewModel, string> title,
        Func<UserControl> createForm, Action<TViewModel> cancel)
    {
        _owner = owner;
        _flagName = flagName;
        _isOpen = isOpen;
        _title = title;
        _createForm = createForm;
        _cancel = cancel;
        owner.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnPropertyChanged;
        _viewModel = e.NewValue as TViewModel;
        if (_viewModel != null)
            _viewModel.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != _flagName || _viewModel == null)
            return;

        if (_isOpen(_viewModel) && _window == null)
            Open(_viewModel);
        else if (!_isOpen(_viewModel) && _window != null)
            _window.Close();
    }

    private void Open(TViewModel viewModel)
    {
        UserControl form = _createForm();
        form.DataContext = viewModel;

        var window = new FormWindow(_title(viewModel), form) { Owner = Window.GetWindow(_owner) };
        _window = window;
        window.Closed += (_, _) =>
        {
            _window = null;
            if (_isOpen(viewModel))      // вікно закрито хрестиком – скасування
                _cancel(viewModel);
        };

        // Показ відкладено, щоб не відкривати модальне вікно всередині встановлення властивості
        _owner.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_window == window)
                window.ShowDialog();
        }));
    }
}
