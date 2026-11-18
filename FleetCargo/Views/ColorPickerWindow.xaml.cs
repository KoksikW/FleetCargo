using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FleetCargo.Services;

namespace FleetCargo.Views;

/// <summary>Вікно вибору акцентного кольору: палітра готових кольорів, RGB-повзунки та код кольору.</summary>
public partial class ColorPickerWindow : Window
{
    private bool _updating;

    public ColorPickerWindow(Color current)
    {
        InitializeComponent();
        BuildPalette();
        SetColor(current);
    }

    /// <summary>Вибраний колір (уже скоригований для читабельності білого тексту).</summary>
    public Color SelectedColor { get; private set; }

    private void BuildPalette()
    {
        // 4 рядки по 12 відтінків різної яскравості та рядок відтінків сірого
        foreach (double lightness in new[] { 0.30, 0.40, 0.50, 0.60 })
        {
            for (int step = 0; step < 12; step++)
                AddSwatch(ThemeService.FromHsl(step * 30, 0.75, lightness));
        }
        for (int step = 0; step < 12; step++)
            AddSwatch(ThemeService.FromHsl(0, 0, 0.12 + step * 0.045));
    }

    private void AddSwatch(Color color)
    {
        var swatch = new Border
        {
            Background = new SolidColorBrush(color),
            Height = 28,
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(3),
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = ThemeService.ToHex(color)
        };
        swatch.MouseLeftButtonUp += (_, _) => SetColor(color);
        PaletteGrid.Children.Add(swatch);
    }

    /// <summary>Встановлює колір і синхронізує повзунки, код кольору та перегляд.</summary>
    private void SetColor(Color color)
    {
        _updating = true;
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        HexBox.Text = ThemeService.ToHex(color);
        _updating = false;
        UpdatePreview(color);
    }

    private void UpdatePreview(Color raw)
    {
        Color adjusted = ThemeService.EnsureReadable(raw);
        SelectedColor = adjusted;
        Preview.Background = new SolidColorBrush(adjusted);
        HintText.Text = adjusted == raw
            ? ""
            : "Колір трохи затемнено, щоб білий текст залишався читабельним.";
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || !IsLoaded && HexBox == null)
            return;

        var color = Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
        _updating = true;
        HexBox.Text = ThemeService.ToHex(color);
        _updating = false;
        UpdatePreview(color);
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || !ThemeService.TryParse(HexBox.Text, out Color color))
            return;

        _updating = true;
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        _updating = false;
        UpdatePreview(color);
    }

    private void OnResetClick(object sender, RoutedEventArgs e) =>
        SetColor(ThemeService.Parse(ThemeService.DefaultAccent));

    private void OnApplyClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
