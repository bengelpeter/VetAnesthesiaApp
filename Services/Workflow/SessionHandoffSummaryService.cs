using System.Text;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public class SessionHandoffSummaryService : ISessionHandoffSummaryService
{
    private readonly IChartConfigurationService _chartConfigurationService;

    public SessionHandoffSummaryService(IChartConfigurationService chartConfigurationService)
    {
        _chartConfigurationService = chartConfigurationService;
    }

    public string Build(
        Animal? animal,
        AnesthesiaSession session,
        ClinicSettings settings,
        IReadOnlyList<AnesthesiaBucket> buckets,
        IReadOnlyList<SessionAlert> alerts,
        SessionCompletionSummary completionSummary)
    {
        var summary = new StringBuilder();
        var latestBucket = buckets.Count > 0 ? buckets[^1] : null;
        var recentNotes = buckets
            .Where(x => !string.IsNullOrWhiteSpace(x.Notes))
            .TakeLast(3)
            .ToList();
        var exportTarget = ClinicExportTargets.Resolve(settings, settings.PreferredExportTargetKey);
        var configuredFields = _chartConfigurationService.GetConfiguredFields(settings);
        var fieldMap = configuredFields.ToDictionary(x => x.Key, x => x.Label);

        summary.AppendLine(exportTarget.NoteTitle);
        summary.AppendLine($"Target workflow: {exportTarget.Label}");

        if (!string.IsNullOrWhiteSpace(settings.ClinicName))
        {
            summary.AppendLine($"Clinic: {settings.ClinicName.Trim()}");
        }

        summary.AppendLine($"Patient: {animal?.Name ?? "Unknown"} ({animal?.Species ?? "Unknown species"})");
        summary.AppendLine($"Procedure: {FormatOrNotRecorded(session.Procedure)}");
        summary.AppendLine($"Start: {session.SessionStartTime:yyyy-MM-dd hh:mm tt}");
        summary.AppendLine($"End: {(session.SessionEndTime.HasValue ? session.SessionEndTime.Value.ToString("yyyy-MM-dd hh:mm tt") : "In progress")}");
        summary.AppendLine($"Monitoring buckets: {buckets.Count}");

        if (!string.IsNullOrWhiteSpace(exportTarget.PrimaryRecordInstruction))
        {
            summary.AppendLine($"Primary record: {exportTarget.PrimaryRecordInstruction}");
        }

        if (latestBucket is not null)
        {
            summary.AppendLine($"Latest bucket: {latestBucket.BucketStartTime:hh:mm tt}");
            summary.AppendLine(
                $"Latest vitals: {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.HeartRate, "HR")} {FormatOrDash(latestBucket.HeartRate)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.RespiratoryRate, "RR")} {FormatOrDash(latestBucket.RespiratoryRate)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.Spo2, "SpO2")} {FormatOrDash(latestBucket.Spo2)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.Etco2, "ETCO2")} {FormatOrDash(latestBucket.Etco2)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.Temperature, "Temp")} {FormatOrDash(latestBucket.Temperature)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.Map, "MAP")} {FormatOrDash(latestBucket.Map)}");
            summary.AppendLine(
                $"Gas settings: {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.IsoPercent, "ISO")} {FormatOrDash(latestBucket.IsoPercent)}, {fieldMap.GetValueOrDefault(Constants.AnesthesiaFieldKeys.OxygenFlowRate, "O2")} {FormatOrDash(latestBucket.OxygenFlowRate)}");
        }
        else
        {
            summary.AppendLine("Latest vitals: No monitoring bucket recorded.");
        }

        if (recentNotes.Count > 0)
        {
            summary.AppendLine("Recent notes:");
            foreach (var noteBucket in recentNotes)
            {
                summary.AppendLine($"- {noteBucket.BucketStartTime:hh:mm tt}: {noteBucket.Notes}");
            }
        }

        if (alerts.Count > 0)
        {
            summary.AppendLine("Active alerts:");
            foreach (var alert in alerts)
            {
                summary.AppendLine($"- {alert.Message}");
            }
        }
        else
        {
            summary.AppendLine("Active alerts: none");
        }

        var incompleteItems = completionSummary.Items.Where(x => !x.IsComplete).ToList();
        if (incompleteItems.Count > 0)
        {
            summary.AppendLine("Open checks:");
            foreach (var item in incompleteItems)
            {
                summary.AppendLine($"- {item.Label}: {item.Message}");
            }
        }
        else
        {
            summary.AppendLine("Open checks: none");
        }

        return summary.ToString().TrimEnd();
    }

    private static string FormatOrDash<T>(T? value) where T : struct =>
        value.HasValue ? value.Value.ToString()! : "-";

    private static string FormatOrNotRecorded(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim();
}
