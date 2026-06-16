using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;
using VetAnesthesiaApp.Services.Voice;

namespace VetAnesthesiaApp.Services.Workflow;

public class WorkflowTelemetryService : IWorkflowTelemetryService
{
    private const string ManualCorrectionEventType = "ManualCorrection";
    private const string SessionCompletionEventType = "SessionCompletion";

    private readonly IAnesthesiaRepository _repository;

    public WorkflowTelemetryService(IAnesthesiaRepository repository)
    {
        _repository = repository;
    }

    public async Task RecordManualCorrectionAsync(Guid sessionId, Guid bucketId, string fieldKey, string detail)
    {
        await _repository.SaveSessionTelemetryEventAsync(new SessionTelemetryEvent
        {
            SessionId = sessionId,
            BucketId = bucketId,
            EventType = ManualCorrectionEventType,
            FieldKey = fieldKey,
            Detail = detail,
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task RecordSessionCompletionChangeAsync(Guid sessionId, bool completed)
    {
        await _repository.SaveSessionTelemetryEventAsync(new SessionTelemetryEvent
        {
            SessionId = sessionId,
            EventType = SessionCompletionEventType,
            Detail = completed ? "Completed" : "Reopened",
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task<SessionTelemetrySummary> BuildSummaryAsync(AnesthesiaSession session)
    {
        var voiceLogs = await _repository.GetVoiceLogsAsync(session.Id);
        var telemetryEvents = await _repository.GetSessionTelemetryEventsAsync(session.Id);

        return new SessionTelemetrySummary
        {
            VoiceAttempts = voiceLogs.Count,
            FailedVoiceAttempts = voiceLogs.Count(x => !x.Applied && !string.Equals(x.MatchedCommandType, nameof(VoiceCommandType.Undo), StringComparison.OrdinalIgnoreCase)),
            UndoUsage = voiceLogs.Count(x => x.Applied && string.Equals(x.MatchedCommandType, nameof(VoiceCommandType.Undo), StringComparison.OrdinalIgnoreCase)),
            ManualCorrections = telemetryEvents.Count(x => x.EventType == ManualCorrectionEventType),
            CompletionEvents = telemetryEvents.Count(x => x.EventType == SessionCompletionEventType),
            TimeToCompletion = session.SessionEndTime.HasValue
                ? session.SessionEndTime.Value - session.SessionStartTime
                : null
        };
    }
}
