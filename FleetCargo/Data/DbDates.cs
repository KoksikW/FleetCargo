using System.Globalization;

namespace FleetCargo.Data;

/// <summary>Перетворення дат: у базі вони зберігаються текстом у форматі рррр-мм-дд.</summary>
public static class DbDates
{
    private const string Format = "yyyy-MM-dd";

    public static string ToDb(DateTime date) => date.ToString(Format, CultureInfo.InvariantCulture);

    public static DateTime FromDb(string text) =>
        DateTime.ParseExact(text, Format, CultureInfo.InvariantCulture);
}
