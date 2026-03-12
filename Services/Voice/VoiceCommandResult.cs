namespace VetAnesthesiaApp.Services.Voice;

public enum VoiceCommandType
{
    Unknown = 0,
    FieldValue = 1,
    Note = 2,
    StartSpecificBucket = 3,
    NextBucket = 4,
    Undo = 5,
    ClearField = 6
}

public class VoiceCommandResult
{
    public VoiceCommandType CommandType { get; set; } = VoiceCommandType.Unknown;

    public string RawTranscript { get; set; } = "";
    public string NormalizedTranscript { get; set; } = "";

    public string? FieldKey { get; set; }
    public string? ParsedValueText { get; set; }
    public decimal? ParsedNumericValue { get; set; }

    public string? NoteText { get; set; }
    public DateTime? ExplicitBucketTime { get; set; }

    public bool IsSuccess { get; set; }
    public string StatusMessage { get; set; } = "";
}