using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public class SessionCompletionEvaluator : ISessionCompletionEvaluator
{
    public SessionCompletionSummary Evaluate(
        AnesthesiaSession? session,
        IReadOnlyList<AnesthesiaBucket> buckets,
        ClinicSettings settings,
        IReadOnlyList<VoiceEntryLog> voiceLogs,
        DateTime now)
    {
        var items = new List<SessionCompletionItem>();

        var hasProcedure = !string.IsNullOrWhiteSpace(session?.Procedure);
        items.Add(new SessionCompletionItem(
            "procedure",
            "Procedure",
            hasProcedure,
            true,
            VetAlertLevel.Warning,
            hasProcedure ? "Procedure is recorded." : "Procedure name is still blank."));

        var hasBuckets = buckets.Count > 0;
        items.Add(new SessionCompletionItem(
            "buckets",
            "Monitoring buckets",
            hasBuckets,
            true,
            VetAlertLevel.Critical,
            hasBuckets ? $"{buckets.Count} monitoring bucket(s) recorded." : "No anesthesia buckets have been recorded yet."));

        var latestBucket = hasBuckets ? buckets[^1] : null;
        var missingFields = latestBucket is null ? new List<string>() : GetMissingLatestVitals(latestBucket);
        items.Add(new SessionCompletionItem(
            "latest-vitals",
            "Latest bucket vitals",
            latestBucket is not null && missingFields.Count == 0,
            true,
            VetAlertLevel.Critical,
            latestBucket is null
                ? "No bucket is available to review."
                : missingFields.Count == 0
                    ? "Latest bucket includes the core anesthesia values."
                    : $"Latest bucket is missing: {string.Join(", ", missingFields)}."));

        var intervalMinutes = latestBucket is null
            ? double.MaxValue
            : (now - latestBucket.BucketStartTime).TotalMinutes;
        var monitoringCurrent = latestBucket is not null && intervalMinutes < settings.MissingVitalsAlertAfterMinutes;
        items.Add(new SessionCompletionItem(
            "monitoring-current",
            "Monitoring interval",
            monitoringCurrent,
            true,
            VetAlertLevel.Warning,
            latestBucket is null
                ? "Monitoring interval cannot be checked until a bucket exists."
                : monitoringCurrent
                    ? $"Latest bucket is within the clinic limit of {settings.MissingVitalsAlertAfterMinutes} minutes."
                    : $"Latest bucket is {Math.Floor(intervalMinutes)} minutes old, which exceeds the clinic limit of {settings.MissingVitalsAlertAfterMinutes} minutes."));

        var sessionClosed = session?.SessionEndTime is not null;
        items.Add(new SessionCompletionItem(
            "session-closed",
            "Procedure complete",
            sessionClosed,
            true,
            VetAlertLevel.Warning,
            sessionClosed
                ? $"Procedure closed at {session!.SessionEndTime:hh:mm tt}."
                : "Procedure is still open. Mark it complete before final handoff."));

        var unresolvedVoiceAttempts = voiceLogs.Count(x => !x.Applied && !string.Equals(x.MatchedCommandType, "Undo", StringComparison.OrdinalIgnoreCase));
        items.Add(new SessionCompletionItem(
            "voice-review",
            "Voice review",
            unresolvedVoiceAttempts == 0,
            false,
            VetAlertLevel.Warning,
            unresolvedVoiceAttempts == 0
                ? "No unresolved voice attempts need review."
                : $"{unresolvedVoiceAttempts} voice attempt(s) were not applied and should be checked."));

        return new SessionCompletionSummary(items);
    }

    private static List<string> GetMissingLatestVitals(AnesthesiaBucket bucket)
    {
        var missing = new List<string>();

        if (!bucket.HeartRate.HasValue)
            missing.Add("HR");
        if (!bucket.RespiratoryRate.HasValue)
            missing.Add("RR");
        if (!bucket.Spo2.HasValue)
            missing.Add("SpO2");
        if (!bucket.Etco2.HasValue)
            missing.Add("ETCO2");
        if (!bucket.Temperature.HasValue)
            missing.Add("Temp");
        if (!bucket.Map.HasValue && !(bucket.SystolicBp.HasValue && bucket.DiastolicBp.HasValue))
            missing.Add("BP/MAP");

        return missing;
    }
}
