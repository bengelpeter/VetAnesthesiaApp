using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Alerts;

public interface ISessionAlertEvaluator
{
    List<SessionAlert> Evaluate(IReadOnlyList<AnesthesiaBucket> buckets, ClinicSettings settings, DateTime now);
}
