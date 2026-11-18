using System.IO;
using System.Text;
using FleetCargo.Data.Repositories;
using FleetCargo.Models;
using FleetCargo.Services;
using Microsoft.Data.Sqlite;

namespace FleetCargo.Data;

/// <summary>
/// Службова самоперевірка репозиторіїв (запускається сполученням Ctrl+Shift+T).
/// Усі зміни виконуються в транзакції, яка скасовується, тому дані бази не змінюються.
/// </summary>
public static class RepositorySelfTest
{
    public static string Run()
    {
        var log = new StringBuilder();
        int passed = 0, total = 0;

        void Check(string name, Func<string> test)
        {
            total++;
            try
            {
                string details = test();
                passed++;
                log.AppendLine($"[OK] {name}: {details}");
            }
            catch (Exception ex)
            {
                log.AppendLine($"[ПОМИЛКА] {name}: {ex.Message}");
            }
        }

        var factory = new SqliteConnectionFactory();
        var vehicles = new VehicleRepository(factory);
        var drivers = new DriverRepository(factory);
        var cargos = new CargoRepository(factory);
        var trips = new TripRepository(factory);

        int vehicleCount = 0, tripCount = 0;

        Check("1. Читання списків", () =>
        {
            vehicleCount = vehicles.GetAll().Count;
            int driverCount = drivers.GetAll().Count;
            int cargoCount = cargos.GetAll().Count;
            tripCount = trips.GetAll().Count;
            if (vehicleCount == 0 || driverCount == 0 || cargoCount == 0 || tripCount == 0)
                throw new InvalidOperationException("одна з таблиць порожня");
            return $"автомобілів {vehicleCount}, водіїв {driverCount}, вантажів {cargoCount}, рейсів {tripCount}";
        });

        using (var uow = new UnitOfWork(factory))
        {
            int id = 0;

            Check("2. Додавання й читання автомобіля", () =>
            {
                id = vehicles.Add(NewVehicle("ТЕСТ 0001"), uow);
                Vehicle v = vehicles.GetById(id, uow) ?? throw new InvalidOperationException("запис не знайдено");
                if (v.PlateNumber != "ТЕСТ 0001")
                    throw new InvalidOperationException("значення не збігається");
                return $"створено запис Id = {id}";
            });

            Check("3. Редагування автомобіля", () =>
            {
                Vehicle v = vehicles.GetById(id, uow)!;
                v.Brand = "Змінено";
                v.Mileage = 5000;
                vehicles.Update(v, uow);
                Vehicle changed = vehicles.GetById(id, uow)!;
                if (changed.Brand != "Змінено" || changed.Mileage != 5000)
                    throw new InvalidOperationException("зміни не збережено");
                return "марку й пробіг оновлено";
            });

            Check("4. Зміна статусу", () =>
            {
                vehicles.SetStatus(id, ResourceStatus.OnTrip, uow);
                string status = vehicles.GetById(id, uow)!.Status;
                if (status != ResourceStatus.OnTrip)
                    throw new InvalidOperationException("статус не змінено");
                return $"новий статус «{status}»";
            });

            Check("5. Видалення автомобіля", () =>
            {
                vehicles.Delete(id, uow);
                if (vehicles.GetById(id, uow) != null)
                    throw new InvalidOperationException("запис не видалено");
                return "запис видалено";
            });

            Check("6. Водії та вантажі: додавання, редагування, видалення", () =>
            {
                int driverId = drivers.Add(new Driver { LastName = "Тестов", FirstName = "Тест", Phone = "000" }, uow);
                Driver d = drivers.GetById(driverId, uow)!;
                d.Phone = "111";
                drivers.Update(d, uow);
                if (drivers.GetById(driverId, uow)!.Phone != "111")
                    throw new InvalidOperationException("водія не оновлено");
                drivers.Delete(driverId, uow);

                int typeId = cargos.GetTypes(uow).First().Id;
                int cargoId = cargos.Add(new Cargo { Name = "Тестовий вантаж", WeightKg = 10, CargoTypeId = typeId }, uow);
                Cargo c = cargos.GetById(cargoId, uow)!;
                c.WeightKg = 20;
                cargos.Update(c, uow);
                if (cargos.GetById(cargoId, uow)!.WeightKg != 20)
                    throw new InvalidOperationException("вантаж не оновлено");
                cargos.Delete(cargoId, uow);
                return "операції виконано успішно";
            });

            Check("7. Захист від SQL-ін'єкції", () =>
            {
                const string evil = "X'; DROP TABLE Vehicle; --";
                int evilId = vehicles.Add(NewVehicle(evil), uow);
                if (vehicles.GetById(evilId, uow)!.PlateNumber != evil)
                    throw new InvalidOperationException("текст змінено");
                if (vehicles.GetAll(uow).Count == 0)
                    throw new InvalidOperationException("таблицю пошкоджено");
                return "значення збережено як текст, таблиця Vehicle не пошкоджена";
            });

            Check("8. Зовнішній ключ: видалення автомобіля з рейсами", () =>
            {
                Trip trip = trips.GetAll(uow).First();
                try
                {
                    vehicles.Delete(trip.VehicleId, uow);
                }
                catch (SqliteException ex)
                {
                    return $"видалення відхилено (код помилки {ex.SqliteErrorCode})";
                }
                throw new InvalidOperationException("видалення дозволено");
            });

            Check("9. Рейс і статуси в одній транзакції", () =>
            {
                Vehicle v = vehicles.GetAll(uow).First(x => x.Status == ResourceStatus.Free);
                Driver d = drivers.GetAll(uow).First(x => x.Status == ResourceStatus.Free);
                Cargo c = cargos.GetAll(uow).First();
                int tripId = trips.Add(new Trip
                {
                    VehicleId = v.Id, DriverId = d.Id, CargoId = c.Id,
                    Origin = "А", Destination = "Б",
                    StartDate = DateTime.Today.AddDays(30), EndDate = DateTime.Today.AddDays(31),
                    Status = TripStatus.InProgress
                }, uow);
                vehicles.SetStatus(v.Id, ResourceStatus.OnTrip, uow);
                drivers.SetStatus(d.Id, ResourceStatus.OnTrip, uow);
                if (trips.GetById(tripId, uow) == null ||
                    vehicles.GetById(v.Id, uow)!.Status != ResourceStatus.OnTrip ||
                    drivers.GetById(d.Id, uow)!.Status != ResourceStatus.OnTrip)
                    throw new InvalidOperationException("зміни не збереглися в межах транзакції");
                return "рейс додано, статуси автомобіля та водія змінено";
            });

            DateTime periodStart = DateTime.Today.AddDays(60);
            DateTime periodEnd = DateTime.Today.AddDays(61);
            var tripService = new TripService(factory, vehicles, drivers, trips);
            int newTripId = 0, newVehicleId = 0, newDriverId = 0;

            Check("10. Доступність на дати (А2)", () =>
            {
                var freeVehicles = vehicles.GetAvailable(periodStart, periodEnd, uow);
                var freeDrivers = drivers.GetAvailable(periodStart, periodEnd, uow);
                Trip active = trips.GetAll(uow).First(t => t.Status == TripStatus.InProgress);
                if (freeVehicles.Any(x => x.Id == active.VehicleId) ||
                    freeDrivers.Any(x => x.Id == active.DriverId))
                    throw new InvalidOperationException("зайнятий ресурс потрапив до вільних");
                if (freeVehicles.Any(x => x.Status != ResourceStatus.Free))
                    throw new InvalidOperationException("у списку є ресурс не зі статусом «Вільний»");
                return $"вільних автомобілів {freeVehicles.Count}, водіїв {freeDrivers.Count}";
            });

            Check("11. Створення рейсу (А1)", () =>
            {
                // Тестовий автомобіль із чинними ТО та страхуванням на весь період
                newVehicleId = vehicles.Add(NewVehicle("ТЕСТ 0011", DateTime.Today.AddDays(365)), uow);
                newDriverId = drivers.GetAvailable(periodStart, periodEnd, uow).First().Id;
                int cargoId = cargos.GetAll(uow).First().Id;

                OperationResult created = tripService.CreateTrip(
                    newVehicleId, newDriverId, cargoId, "А", "Б", periodStart, periodEnd, uow);
                if (!created.Success)
                    throw new InvalidOperationException(created.Error);
                newTripId = created.Id;
                if (vehicles.GetById(newVehicleId, uow)!.Status != ResourceStatus.OnTrip)
                    throw new InvalidOperationException("статус автомобіля не змінено");

                OperationResult again = tripService.CreateTrip(
                    newVehicleId, newDriverId, cargoId, "А", "Б", periodStart, periodEnd, uow);
                if (again.Success || again.Error != "Ресурс уже в дорозі")
                    throw new InvalidOperationException("повторне створення не відхилено");

                OperationResult invalid = tripService.CreateTrip(
                    newVehicleId, newDriverId, cargoId, "", "Б", periodEnd, periodStart, uow);
                if (invalid.Success)
                    throw new InvalidOperationException("некоректні дані прийнято");
                return $"рейс створено; повторне створення відхилено: «{again.Error}»";
            });

            Check("12. Контроль ТО та страхування під час створення рейсу", () =>
            {
                int oldServiceId = vehicles.Add(NewVehicle("ТЕСТ 0012", DateTime.Today.AddDays(365),
                    serviceDate: DateTime.Today.AddDays(-1)), uow);
                int oldInsuranceId = vehicles.Add(NewVehicle("ТЕСТ 0013", DateTime.Today.AddDays(365),
                    insuranceDate: DateTime.Today.AddDays(10)), uow);
                int driverId = drivers.GetAvailable(periodStart, periodEnd, uow).First().Id;
                int cargoId = cargos.GetAll(uow).First().Id;

                OperationResult byService = tripService.CreateTrip(
                    oldServiceId, driverId, cargoId, "А", "Б", periodStart, periodEnd, uow);
                OperationResult byInsurance = tripService.CreateTrip(
                    oldInsuranceId, driverId, cargoId, "А", "Б", periodStart, periodEnd, uow);
                if (byService.Success || byInsurance.Success)
                    throw new InvalidOperationException("рейс створено попри недійсні ТО або страхування");
                return "рейс відхилено для автомобіля з простроченим ТО та зі страхуванням, що спливає раніше";
            });

            Check("13. Завершення рейсу (А4)", () =>
            {
                OperationResult done = tripService.CompleteTrip(newTripId, uow);
                if (!done.Success)
                    throw new InvalidOperationException(done.Error);
                if (vehicles.GetById(newVehicleId, uow)!.Status != ResourceStatus.Free ||
                    drivers.GetById(newDriverId, uow)!.Status != ResourceStatus.Free)
                    throw new InvalidOperationException("статуси не повернуто до «Вільний»");

                OperationResult second = tripService.CompleteTrip(newTripId, uow);
                if (second.Success)
                    throw new InvalidOperationException("повторне завершення дозволено");
                return $"ресурси вільні; повторне завершення відхилено: «{second.Error}»";
            });

            Check("14. Попередження про ТО та страхування (А3)", () =>
            {
                var alertService = new AlertService();
                DateTime today = DateTime.Today;
                if (alertService.GetLevel(today.AddDays(-1), today) != AlertLevel.Expired)
                    throw new InvalidOperationException("прострочена дата");
                if (alertService.GetLevel(today.AddDays(14), today) != AlertLevel.Warning)
                    throw new InvalidOperationException("межа 14 днів");
                if (alertService.GetLevel(today.AddDays(15), today) != AlertLevel.None)
                    throw new InvalidOperationException("дата поза порогом");
                int count = alertService.BuildAlerts(vehicles.GetAll(uow), today).Count;
                return $"пороги визначено правильно; попереджень у списку: {count}";
            });

            Check("15. Пошук і фільтрація", () =>
            {
                // Текст без урахування регістру (кирилиця) та точний збіг
                var byPart = drivers.Search(new DriverFilter { Text = "мельник" }, uow);
                var byExact = drivers.Search(new DriverFilter { Text = "МЕЛЬНИК", ExactMatch = true }, uow);
                if (byPart.Count == 0 || byExact.Count == 0)
                    throw new InvalidOperationException("пошук за прізвищем без урахування регістру не знайшов запис");

                // Спеціальні символи LIKE екрануються
                if (vehicles.Search(new VehicleFilter { Text = "%" }, uow).Count != 0)
                    throw new InvalidOperationException("символ % оброблено як шаблон");

                // Числовий діапазон і статус
                var inRange = vehicles.Search(new VehicleFilter { MileageFrom = 100000, MileageTo = 250000 }, uow);
                if (inRange.Any(v => v.Mileage < 100000 || v.Mileage > 250000))
                    throw new InvalidOperationException("у результаті є записи поза діапазоном пробігу");
                var onTrip = vehicles.Search(new VehicleFilter { Status = ResourceStatus.OnTrip }, uow);
                if (onTrip.Any(v => v.Status != ResourceStatus.OnTrip))
                    throw new InvalidOperationException("фільтр за статусом працює неправильно");

                // Рейси за статусом
                var done = trips.Search(new TripFilter { Status = TripStatus.Completed }, uow);
                if (done.Any(t => t.Status != TripStatus.Completed))
                    throw new InvalidOperationException("фільтр рейсів за статусом працює неправильно");
                return $"водіїв за прізвищем: {byPart.Count}; автомобілів у діапазоні пробігу: {inRange.Count}; завершених рейсів: {done.Count}";
            });

            Check("16. Фото та характеристика", () =>
            {
                Vehicle v = NewVehicle("ТЕСТ 0016");
                v.PhotoPath = "test.jpg";
                v.Notes = "Примітка до автомобіля";
                Vehicle savedVehicle = vehicles.GetById(vehicles.Add(v, uow), uow)!;

                int driverId = drivers.Add(new Driver
                {
                    LastName = "Тестов", FirstName = "Фото", PhotoPath = "driver.png", Notes = "Характеристика"
                }, uow);
                Driver savedDriver = drivers.GetById(driverId, uow)!;

                if (savedVehicle.PhotoPath != "test.jpg" || savedVehicle.Notes != "Примітка до автомобіля" ||
                    savedDriver.PhotoPath != "driver.png" || savedDriver.Notes != "Характеристика")
                    throw new InvalidOperationException("шлях до фото або характеристика не збереглися");
                if (PhotoService.Validate(@"C:\Temp\document.txt") == null)
                    throw new InvalidOperationException("недопустимий формат файлу прийнято");
                return "шлях до фото та характеристика зберігаються; недопустимий формат файлу відхиляється";
            });

            Check("17. Звіти та експорт у Excel", () =>
            {
                var reportService = new ReportService(trips, vehicles, new AlertService());
                ReportResult byPeriod = reportService.Build(ReportService.TripsReportName, null, null);
                ReportResult alerts = reportService.Build(ReportService.AlertsReportName, null, null);
                if (!byPeriod.Success || !alerts.Success)
                    throw new InvalidOperationException("звіт не сформовано");
                if (byPeriod.Table!.Rows.Count != trips.GetAll().Count)
                    throw new InvalidOperationException("кількість рядків звіту не збігається з кількістю рейсів");

                string reportPath = Path.Combine(Path.GetTempPath(), $"fleetcargo_report_{Guid.NewGuid():N}.xlsx");
                string routePath = Path.Combine(Path.GetTempPath(), $"fleetcargo_route_{Guid.NewGuid():N}.xlsx");
                try
                {
                    var exportService = new ExportService(vehicles, drivers, cargos, trips);
                    exportService.ExportTable("Звіт", byPeriod.Table, reportPath);
                    OperationResult route = exportService.ExportRouteSheet(trips.GetAll().First().Id, routePath);
                    if (!route.Success)
                        throw new InvalidOperationException(route.Error);
                    if (!File.Exists(reportPath) || new FileInfo(reportPath).Length == 0 ||
                        !File.Exists(routePath) || new FileInfo(routePath).Length == 0)
                        throw new InvalidOperationException("файли Excel не створено");
                    return $"рядків у звіті про рейси: {byPeriod.Table.Rows.Count}; файли Excel (звіт і маршрутний лист) створено";
                }
                finally
                {
                    File.Delete(reportPath);
                    File.Delete(routePath);
                }
            });

            Check("18. Збереження налаштувань інтерфейсу (XML)", () =>
            {
                string path = Path.Combine(Path.GetTempPath(), $"fleetcargo_settings_{Guid.NewGuid():N}.xml");
                try
                {
                    var original = new UserSettings
                    {
                        WindowLeft = 120, WindowTop = 80, WindowWidth = 1000, WindowHeight = 640,
                        WindowState = "Normal", SelectedTab = 3, AccentColor = "#1E8E5A",
                        ReportType = ReportService.AlertsReportName,
                        ReportPeriodStart = new DateTime(2026, 11, 1), ReportPeriodEnd = new DateTime(2026, 11, 30)
                    };
                    if (!SettingsService.Write(original, path))
                        throw new InvalidOperationException("файл налаштувань не записано");

                    UserSettings loaded = SettingsService.Read(path);
                    if (loaded.WindowWidth != 1000 || loaded.WindowState != "Normal" || loaded.SelectedTab != 3 ||
                        loaded.AccentColor != "#1E8E5A" || loaded.ReportType != ReportService.AlertsReportName ||
                        loaded.ReportPeriodStart != new DateTime(2026, 11, 1) ||
                        loaded.ReportPeriodEnd != new DateTime(2026, 11, 30))
                        throw new InvalidOperationException("прочитані значення не збігаються із записаними");

                    File.WriteAllText(path, "<не xml");
                    UserSettings fallback = SettingsService.Read(path);
                    if (fallback.AccentColor != ThemeService.DefaultAccent || fallback.SelectedTab != 0)
                        throw new InvalidOperationException("пошкоджений файл не замінено значеннями за замовчуванням");
                    return "налаштування записуються й читаються; пошкоджений файл замінюється значеннями за замовчуванням";
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }   // Commit не викликано: усі зміни вище скасовано

        Check("19. Відкат транзакції: дані не змінилися", () =>
        {
            int vehiclesNow = vehicles.GetAll().Count;
            int tripsNow = trips.GetAll().Count;
            if (vehiclesNow != vehicleCount || tripsNow != tripCount)
                throw new InvalidOperationException("кількість записів змінилася");
            return $"автомобілів {vehiclesNow}, рейсів {tripsNow} (без змін)";
        });

        return $"Пройдено перевірок: {passed} із {total}\n\n{log}";
    }

    private static Vehicle NewVehicle(string plate, DateTime? validUntil = null,
        DateTime? serviceDate = null, DateTime? insuranceDate = null) => new()
    {
        PlateNumber = plate,
        Brand = "Test",
        Model = "Test",
        Mileage = 1000,
        NextServiceDate = serviceDate ?? validUntil ?? DateTime.Today.AddDays(30),
        InsuranceEndDate = insuranceDate ?? validUntil ?? DateTime.Today.AddDays(60),
        Status = ResourceStatus.Free
    };
}
