using System.Data;
using System.IO;
using System.Windows.Input;
using FleetCargo.Services;

namespace FleetCargo.ViewModels;

/// <summary>Модуль «Звіти»: вибір типу звіту та періоду, показ таблиці, експорт у Excel.</summary>
public class ReportsViewModel : ViewModelBase
{
    private readonly ReportService _reportService;
    private readonly ExportService _exportService;

    private DateTime? _periodStart;
    private DateTime? _periodEnd;
    private string _selectedReportType = ReportService.TripsReportName;
    private DataTable? _table;
    private DataView? _reportRows;
    private string _summaryText = "";

    public ReportsViewModel(ReportService reportService, ExportService exportService)
    {
        _reportService = reportService;
        _exportService = exportService;

        GenerateCommand = new RelayCommand(Generate);
        ExportCommand = new RelayCommand(Export, () => _table != null);
    }

    public IReadOnlyList<string> ReportTypes { get; } = ReportService.ReportNames;

    public ICommand GenerateCommand { get; }
    public ICommand ExportCommand { get; }

    public DateTime? PeriodStart { get => _periodStart; set => SetProperty(ref _periodStart, value); }
    public DateTime? PeriodEnd { get => _periodEnd; set => SetProperty(ref _periodEnd, value); }
    public string SelectedReportType { get => _selectedReportType; set => SetProperty(ref _selectedReportType, value); }
    public DataView? ReportRows { get => _reportRows; private set => SetProperty(ref _reportRows, value); }
    public string SummaryText { get => _summaryText; private set => SetProperty(ref _summaryText, value); }

    private void Generate()
    {
        ReportResult result = _reportService.Build(SelectedReportType, PeriodStart, PeriodEnd);
        if (!result.Success)
        {
            Dialogs.Error(result.Error);
            return;
        }

        _table = result.Table;
        ReportRows = result.Table!.DefaultView;
        SummaryText = result.Summary;
    }

    private void Export()
    {
        if (_table == null)
            return;

        string? path = Dialogs.PickSavePath($"Звіт_{DateTime.Today:yyyyMMdd}");
        if (path == null)
            return;

        try
        {
            _exportService.ExportTable(SelectedReportType, _table, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Dialogs.Error("Не вдалося зберегти файл: " + ex.Message);
            return;
        }

        Dialogs.OfferOpen(path);
    }
}
