namespace FleetCargo.Data;

/// <summary>
/// Динамічно формує умову WHERE за заповненими критеріями пошуку.
/// Текст умов складається лише з фіксованих фрагментів, а всі значення передаються параметрами.
/// </summary>
public class WhereBuilder
{
    private readonly List<string> _conditions = new();
    private readonly List<(string Name, object? Value)> _parameters = new();

    /// <summary>Готовий фрагмент " WHERE ..." або порожній рядок, якщо критеріїв немає.</summary>
    public string Sql => _conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", _conditions);

    public (string Name, object? Value)[] Parameters => _parameters.ToArray();

    /// <summary>Текстовий критерій по кількох стовпцях: частковий (LIKE) або точний збіг.</summary>
    public void AddText(string[] columns, string? text, bool exact)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string value = text.Trim().ToLowerInvariant();
        string condition;
        if (exact)
        {
            condition = string.Join(" OR ", columns.Select(c => $"ulower({c}) = $text"));
        }
        else
        {
            condition = string.Join(" OR ", columns.Select(c => $"ulower({c}) LIKE $text ESCAPE '\\'"));
            value = "%" + EscapeLike(value) + "%";
        }

        _conditions.Add("(" + condition + ")");
        _parameters.Add(("$text", value));
    }

    /// <summary>Додає умову, лише якщо значення задано.</summary>
    public void Add(string condition, string parameterName, object? value)
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            return;

        _conditions.Add(condition);
        _parameters.Add((parameterName, value));
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
