using FleetCargo.Models;

namespace FleetCargo.Services;

/// <summary>Один елемент списку попереджень.</summary>
public record AlertItem(Vehicle Vehicle, string Kind, DateTime Date, int Days)
{
    public string Text => Days switch
    {
        < 0 => $"{Vehicle.PlateNumber} — {Kind}: прострочено на {-Days} дн. ({Date:dd.MM.yyyy})",
        0 => $"{Vehicle.PlateNumber} — {Kind}: термін спливає сьогодні",
        _ => $"{Vehicle.PlateNumber} — {Kind}: через {Days} дн. ({Date:dd.MM.yyyy})"
    };
}

/// <summary>Попередження про ТО та страхування (алгоритм А3).</summary>
public class AlertService
{
    /// <summary>Поріг попередження, днів.</summary>
    public const int ThresholdDays = 14;

    public AlertLevel GetLevel(DateTime date, DateTime today)
    {
        int days = (date.Date - today.Date).Days;
        if (days < 0)
            return AlertLevel.Expired;
        return days <= ThresholdDays ? AlertLevel.Warning : AlertLevel.None;
    }

    /// <summary>Встановлює кожному автомобілю рівень попередження (більший із двох термінів).</summary>
    public void Apply(IEnumerable<Vehicle> vehicles, DateTime today)
    {
        foreach (Vehicle v in vehicles)
        {
            AlertLevel service = GetLevel(v.NextServiceDate, today);
            AlertLevel insurance = GetLevel(v.InsuranceEndDate, today);
            v.AlertLevel = (AlertLevel)Math.Max((int)service, (int)insurance);
        }
    }

    /// <summary>Формує перелік попереджень, відсортований за кількістю днів.</summary>
    public List<AlertItem> BuildAlerts(IEnumerable<Vehicle> vehicles, DateTime today)
    {
        var list = new List<AlertItem>();
        foreach (Vehicle v in vehicles)
        {
            int serviceDays = (v.NextServiceDate.Date - today.Date).Days;
            if (serviceDays <= ThresholdDays)
                list.Add(new AlertItem(v, "ТО", v.NextServiceDate, serviceDays));

            int insuranceDays = (v.InsuranceEndDate.Date - today.Date).Days;
            if (insuranceDays <= ThresholdDays)
                list.Add(new AlertItem(v, "Страхування", v.InsuranceEndDate, insuranceDays));
        }
        return list.OrderBy(a => a.Days).ToList();
    }
}
