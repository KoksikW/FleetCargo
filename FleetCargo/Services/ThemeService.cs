using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FleetCargo.Services;

/// <summary>
/// Колірна тема інтерфейсу: користувач вибирає акцентний колір, а колір тла вікон
/// обчислюється як його дуже світлий відтінок. Занадто світлі кольори автоматично
/// затемнюються, щоб білий текст на акцентних елементах лишався читабельним.
/// </summary>
public static class ThemeService
{
    public const string DefaultAccent = "#1F6FEB";

    /// <summary>Застосовує акцентний колір, замінюючи динамічні ресурси AccentBrush та AppBackground.</summary>
    public static void Apply(string? hex)
    {
        Color accent = EnsureReadable(Parse(hex));
        ResourceDictionary resources = Application.Current.Resources;
        resources["AccentBrush"] = new SolidColorBrush(accent);
        resources["AppBackground"] = new SolidColorBrush(Tint(accent, 0.94));
    }

    /// <summary>Розбирає колір у форматі #RRGGBB; для некоректного значення повертає колір за замовчуванням.</summary>
    public static Color Parse(string? hex) =>
        TryParse(hex, out Color color) ? color : (Color)ColorConverter.ConvertFromString(DefaultAccent);

    public static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        string text = hex.Trim().TrimStart('#');
        if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
            return false;

        color = Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>Затемнює колір, доки контраст білого тексту на ньому не досягне 3:1.</summary>
    public static Color EnsureReadable(Color color)
    {
        for (int i = 0; i < 40 && ContrastWithWhite(color) < 3.0; i++)
            color = Color.FromRgb((byte)(color.R * 0.92), (byte)(color.G * 0.92), (byte)(color.B * 0.92));
        return color;
    }

    public static double ContrastWithWhite(Color color) => 1.05 / (Luminance(color) + 0.05);

    /// <summary>Змішує колір із білим: amount = 0 – вихідний колір, 1 – білий.</summary>
    public static Color Tint(Color color, double amount) => Color.FromRgb(
        (byte)Math.Round(color.R * (1 - amount) + 255 * amount),
        (byte)Math.Round(color.G * (1 - amount) + 255 * amount),
        (byte)Math.Round(color.B * (1 - amount) + 255 * amount));

    /// <summary>Перетворює колір із моделі HSL (відтінок 0–360, насиченість і яскравість 0–1).</summary>
    public static Color FromHsl(double hue, double saturation, double lightness)
    {
        double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = lightness - c / 2;
        (double r, double g, double b) = hue switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };
        return Color.FromRgb(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);

    private static double Luminance(Color color) =>
        0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);

    private static double Channel(byte value)
    {
        double c = value / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
