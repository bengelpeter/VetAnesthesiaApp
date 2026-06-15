using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Workflow;

namespace VetAnesthesiaApp.Tests;

public class SessionWorkflowTests
{
    private readonly SessionCompletionEvaluator _completionEvaluator = new();
    private readonly SessionHandoffSummaryService _handoffSummaryService = new();

    [Fact]
    public void Evaluate_ReturnsReadyForHandoff_WhenCoreChecksAreSatisfied()
    {
        var session = new AnesthesiaSession
        {
            Procedure = "Spay",
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 9, 45, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 40, 0),
                HeartRate = 92,
                RespiratoryRate = 14,
                Spo2 = 98,
                Etco2 = 37,
                Temperature = 99.4m,
                Map = 72,
                IsoPercent = 1.5m,
                OxygenFlowRate = 2.0m
            }
        };

        var summary = _completionEvaluator.Evaluate(
            session,
            buckets,
            new ClinicSettings { MissingVitalsAlertAfterMinutes = 10 },
            Array.Empty<VoiceEntryLog>(),
            new DateTime(2026, 6, 15, 9, 44, 0));

        Assert.True(summary.IsReadyForHandoff);
        Assert.DoesNotContain(summary.Items, x => x.BlocksHandoff && !x.IsComplete);
    }

    [Fact]
    public void Evaluate_FlagsMissingLatestVitalsAndOpenProcedure()
    {
        var session = new AnesthesiaSession
        {
            Procedure = "",
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
                HeartRate = 88
            }
        };
        var voiceLogs = new[]
        {
            new VoiceEntryLog { Applied = false, MatchedCommandType = "FieldValue", StatusMessage = "No command match." }
        };

        var summary = _completionEvaluator.Evaluate(
            session,
            buckets,
            new ClinicSettings { MissingVitalsAlertAfterMinutes = 5 },
            voiceLogs,
            new DateTime(2026, 6, 15, 9, 10, 0));

        Assert.False(summary.IsReadyForHandoff);
        Assert.Contains(summary.Items, x => x.Key == "procedure" && !x.IsComplete);
        Assert.Contains(summary.Items, x => x.Key == "latest-vitals" && x.Message.Contains("SpO2"));
        Assert.Contains(summary.Items, x => x.Key == "session-closed" && !x.IsComplete);
        Assert.Contains(summary.Items, x => x.Key == "voice-review" && !x.IsComplete);
    }

    [Fact]
    public void Build_IncludesAlertsAndOpenChecks()
    {
        var animal = new Animal { Name = "Ember", Species = "Canine" };
        var session = new AnesthesiaSession
        {
            Procedure = "Dental",
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 9, 50, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 45, 0),
                HeartRate = 90,
                RespiratoryRate = 16,
                Spo2 = 97,
                Etco2 = 38,
                Temperature = 99.1m,
                Map = 68,
                IsoPercent = 1.4m,
                OxygenFlowRate = 2.0m,
                Notes = "recovering quietly"
            }
        };
        var alerts = new[]
        {
            new SessionAlert(VetAlertLevel.Warning, "MAP has remained below the clinic threshold.")
        };
        var completion = _completionEvaluator.Evaluate(
            session,
            buckets,
            new ClinicSettings(),
            Array.Empty<VoiceEntryLog>(),
            new DateTime(2026, 6, 15, 9, 49, 0));

        var summary = _handoffSummaryService.Build(animal, session, buckets, alerts, completion);

        Assert.Contains("VetPulse anesthesia handoff", summary);
        Assert.Contains("Patient: Ember (Canine)", summary);
        Assert.Contains("Procedure: Dental", summary);
        Assert.Contains("Active alerts:", summary);
        Assert.Contains("MAP has remained below the clinic threshold.", summary);
        Assert.Contains("Open checks:", summary);
    }
}
