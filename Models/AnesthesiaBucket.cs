using SQLite;

namespace VetAnesthesiaApp.Models;

public class AnesthesiaBucket
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }

    public DateTime BucketStartTime { get; set; }
    public DateTime? BucketEndTime { get; set; }

    public decimal? IsoPercent { get; set; }
    public decimal? OxygenFlowRate { get; set; }
    public decimal? Etco2 { get; set; }
    public int? Spo2 { get; set; }
    public decimal? Temperature { get; set; }

    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? Map { get; set; }

    public string? Notes { get; set; }
}