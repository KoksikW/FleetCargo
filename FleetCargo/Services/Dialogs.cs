using System.Windows;

namespace FleetCargo.Services;

/// <summary>Діалоги повідомлень і підтверджень.</summary>
public static class Dialogs
{
    private const string Caption = "Fleet & Cargo";

    public static void Info(string message) =>
        MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Error(string message) =>
        MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, Caption, MessageBoxButton.YesNo, MessageBoxImage.Question)
        == MessageBoxResult.Yes;

    /// <summary>Діалог вибору зображення; повертає шлях до файлу або null.</summary>
    public static string? PickImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Виберіть фото",
            Filter = "Зображення (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>Діалог вибору місця збереження файлу Excel; повертає шлях або null.</summary>
    public static string? PickSavePath(string defaultName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Зберегти файл Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = defaultName,
            DefaultExt = ".xlsx",
            AddExtension = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>Повідомляє про збереження файлу та пропонує відкрити його.</summary>
    public static void OfferOpen(string path)
    {
        if (!Confirm($"Файл збережено:\n{path}\n\nВідкрити його?"))
            return;
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Error("Не вдалося відкрити файл. Відкрийте його вручну.");
        }
    }
}
