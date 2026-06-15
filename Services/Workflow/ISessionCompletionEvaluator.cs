using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface ISessionCompletionEvaluator
{
    SessionCompletionSummary Evaluate(
        AnesthesiaSession? session,
        IReadOnlyList<AnesthesiaBucket> buckets,
        ClinicSettings settings,
        IReadOnlyList<VoiceEntryLog> voiceLogs,
        DateTime now);
}
