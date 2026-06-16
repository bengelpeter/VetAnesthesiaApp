using SQLite;

namespace VetAnesthesiaApp.Models;

public class ClinicSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public int MonitoringIntervalMinutes { get; set; } = 5;
    public int MissingVitalsAlertAfterMinutes { get; set; } = 10;
    public int Spo2LowThreshold { get; set; } = 95;
    public int MapLowThreshold { get; set; } = 60;
    public decimal TemperatureDropAlertDelta { get; set; } = 1.0m;
    public bool EnableAlerts { get; set; } = true;
    public string ClinicName { get; set; } = "";
    public string PreferredExportTargetKey { get; set; } = "ClinicChartNote";
}
