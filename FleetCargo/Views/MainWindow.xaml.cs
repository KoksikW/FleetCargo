using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FleetCargo.Data;
using FleetCargo.Services;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

public partial class MainWindow : Window
{
    private int _lastTab;

    public MainWindow()
    {
        InitializeComponent();

        // Розмір вікна не перевищує 95 % робочої області екрана
        Rect area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width * 0.95);
        Height = Math.Min(Height, area.Height * 0.95);

        ApplyWindowSettings(SettingsService.Current);
    }

    /// <summary>Відновлює положення, розмір і стан вікна з минулого сеансу.</summary>
    private void ApplyWindowSettings(UserSettings settings)
    {
        if (settings.WindowWidth >= MinWidth && settings.WindowHeight >= MinHeight)
        {
            Width = Math.Min(settings.WindowWidth, SystemParameters.VirtualScreenWidth);
            Height = Math.Min(settings.WindowHeight, SystemParameters.VirtualScreenHeight);

            // Положення відновлюється, лише якщо вікно потрапляє на наявні екрани
            double left = SystemParameters.VirtualScreenLeft;
            double top = SystemParameters.VirtualScreenTop;
            double right = left + SystemParameters.VirtualScreenWidth;
            double bottom = top + SystemParameters.VirtualScreenHeight;
            bool visible = settings.WindowLeft + 100 > left && settings.WindowLeft < right - 100 &&
                           settings.WindowTop >= top - 10 && settings.WindowTop < bottom - 100;
            if (visible)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
        }

        WindowState = settings.WindowState == nameof(System.Windows.WindowState.Maximized)
            ? WindowState.Maximized
            : WindowState.Normal;
    }

    /// <summary>Під час закриття вікна зберігає налаштування інтерфейсу у файл XML.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        UserSettings settings = SettingsService.Current;

        // Для розгорнутого вікна запам'ятовуються розміри звичайного стану
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;
        settings.WindowLeft = bounds.Left;
        settings.WindowTop = bounds.Top;
        settings.WindowWidth = bounds.Width;
        settings.WindowHeight = bounds.Height;
        settings.WindowState = WindowState == WindowState.Maximized
            ? nameof(System.Windows.WindowState.Maximized)
            : nameof(System.Windows.WindowState.Normal);

        (DataContext as MainViewModel)?.CollectSettings(settings);
        SettingsService.Save();

        base.OnClosing(e);
    }

    /// <summary>
    /// Вкладка «Авторство» не відкривається як звичайна: замість перемикання вкладки
    /// показується вікно з відомостями про автора, а попередня вкладка залишається вибраною.
    /// </summary>
    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, MainTabs))
            return;     // подія вкладеного елемента (список, таблиця тощо)

        if (MainTabs.SelectedIndex == MainViewModel.AuthorTabIndex)
        {
            MainTabs.SelectedIndex = _lastTab;
            if ((DataContext as MainViewModel)?.AuthorModule is AuthorViewModel author)
                new AuthorWindow(author) { Owner = this }.ShowDialog();
        }
        else if (MainTabs.SelectedIndex >= 0)
        {
            _lastTab = MainTabs.SelectedIndex;
        }
    }

    /// <summary>Відкриває палітру й застосовує вибраний акцентний колір.</summary>
    private void OnColorClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var dialog = new ColorPickerWindow(ThemeService.Parse(viewModel.AccentColor)) { Owner = this };
        if (dialog.ShowDialog() == true)
            viewModel.AccentColor = ThemeService.ToHex(dialog.SelectedColor);
    }

    /// <summary>Службова самоперевірка репозиторіїв: Ctrl+Shift+T.</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            e.Handled = true;
            MessageBox.Show(this, RepositorySelfTest.Run(), "Самоперевірка репозиторіїв",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        base.OnPreviewKeyDown(e);
    }
}
