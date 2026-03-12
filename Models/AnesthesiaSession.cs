using SQLite;

namespace VetAnesthesiaApp.Models;

public class AnesthesiaSession
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnimalId { get; set; }

    public string Procedure { get; set; } = "";
    public int? AsaStatus { get; set; }

    public DateTime SessionStartTime { get; set; } = DateTime.Now;
    public DateTime? SessionEndTime { get; set; }

    public string? Comments { get; set; }

    public bool IsListening { get; set; }
    public Guid? ActiveBucketId { get; set; }
}