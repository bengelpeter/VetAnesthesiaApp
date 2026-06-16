namespace VetAnesthesiaApp.Services.Workflow;

public sealed class SessionTelemetrySummary
{
    public int VoiceAttempts { get; init; }
    public int FailedVoiceAttempts { get; init; }
    public int UndoUsage { get; init; }
    public int ManualCorrections { get; init; }
    public int CompletionEvents { get; init; }
    public TimeSpan? TimeToCompletion { get; init; }
}
