using SQLite;

namespace VetAnesthesiaApp.Models;

public class VoiceEntryLog
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }
    public Guid? BucketId { get; set; }

    public DateTime SpokenAt { get; set; } = DateTime.Now;

    public string RawTranscript { get; set; } = "";
    public string NormalizedTranscript { get; set; } = "";

    public string? MatchedCommandType { get; set; }
    public string? MatchedFieldKey { get; set; }
    public string? ParsedValueText { get; set; }
    public decimal? ParsedNumericValue { get; set; }

    public bool Applied { get; set; }
    public string? StatusMessage { get; set; }
}