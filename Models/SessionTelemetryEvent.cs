using SQLite;

namespace VetAnesthesiaApp.Models;

public class SessionTelemetryEvent
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }
    public Guid? BucketId { get; set; }

    public string EventType { get; set; } = "";
    public string? FieldKey { get; set; }
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
