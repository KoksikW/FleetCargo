using System.Windows.Input;

namespace FleetCargo.ViewModels;

/// <summary>Проста реалізація команди для кнопок (шаблон MVVM).</summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), Wrap(canExecute))
    {
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    private static Func<object?, bool>? Wrap(Func<bool>? canExecute)
    {
        if (canExecute == null)
            return null;
        return _ => canExecute();
    }
}
