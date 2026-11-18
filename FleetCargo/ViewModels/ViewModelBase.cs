using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FleetCargo.ViewModels;

/// <summary>Базовий клас моделей подання: сповіщення інтерфейсу про зміну властивостей.</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>Розбирає необов'язкове невід'ємне ціле: порожній рядок дає null, помилка – false.</summary>
    protected static bool TryParseNonNegative(string? text, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (int.TryParse(text.Trim(), out int number) && number >= 0)
        {
            value = number;
            return true;
        }
        return false;
    }

    protected bool SetProperty<T>(ref T field, T value,
        [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
