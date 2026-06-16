using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface IWorkflowTelemetryService
{
    Task RecordManualCorrectionAsync(Guid sessionId, Guid bucketId, string fieldKey, string detail);
    Task RecordSessionCompletionChangeAsync(Guid sessionId, bool completed);
    Task<SessionTelemetrySummary> BuildSummaryAsync(AnesthesiaSession session);
}
