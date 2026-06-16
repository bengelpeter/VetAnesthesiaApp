using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface ISessionHandoffSummaryService
{
    string Build(
        Animal? animal,
        AnesthesiaSession session,
        ClinicSettings settings,
        IReadOnlyList<AnesthesiaBucket> buckets,
        IReadOnlyList<SessionAlert> alerts,
        SessionCompletionSummary completionSummary);
}
