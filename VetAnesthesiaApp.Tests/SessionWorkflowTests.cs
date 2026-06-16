using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Workflow;

namespace VetAnesthesiaApp.Tests;

public class SessionWorkflowTests
{
    private readonly IChartConfigurationService _chartConfigurationService = new ChartConfigurationService();
    private readonly SessionCompletionEvaluator _completionEvaluator;
    private readonly SessionHandoffSummaryService _handoffSummaryService;
    private readonly SessionStructuredExportService _structuredExportService;

    public SessionWorkflowTests()
    {
        _completionEvaluator = new SessionCompletionEvaluator(_chartConfigurationService);
        _handoffSummaryService = new SessionHandoffSummaryService(_chartConfigurationService);
        _structuredExportService = new SessionStructuredExportService(_chartConfigurationService);
    }

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

        var summary = _handoffSummaryService.Build(
            animal,
            session,
            new ClinicSettings
            {
                ClinicName = "Main Street Vet",
                PreferredExportTargetKey = ClinicExportTargets.ClinicChartNote,
                ClinicChartExportLabel = "SOAP note",
                ClinicChartExportNoteTitle = "Surgery anesthesia SOAP note",
                ClinicChartExportDescription = "Paste this SOAP note into the patient chart."
            },
            buckets,
            alerts,
            completion);

        Assert.Contains("Surgery anesthesia SOAP note", summary);
        Assert.Contains("Target workflow: SOAP note", summary);
        Assert.Contains("Clinic: Main Street Vet", summary);
        Assert.Contains("Patient: Ember (Canine)", summary);
        Assert.Contains("Procedure: Dental", summary);
        Assert.Contains("Active alerts:", summary);
        Assert.Contains("MAP has remained below the clinic threshold.", summary);
        Assert.Contains("Open checks:", summary);
    }

    [Fact]
    public void Build_UsesPdfAttachmentNoteFormat_WhenTargetRequestsPdfWorkflow()
    {
        var animal = new Animal { Name = "Milo", Species = "Feline" };
        var session = new AnesthesiaSession
        {
            Procedure = "Mass removal",
            SessionStartTime = new DateTime(2026, 6, 15, 10, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 10, 40, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 10, 35, 0),
                HeartRate = 102,
                RespiratoryRate = 18,
                Spo2 = 99,
                Etco2 = 35,
                Temperature = 100.1m,
                Map = 74
            }
        };
        var completion = _completionEvaluator.Evaluate(
            session,
            buckets,
            new ClinicSettings(),
            Array.Empty<VoiceEntryLog>(),
            new DateTime(2026, 6, 15, 10, 39, 0));

        var summary = _handoffSummaryService.Build(
            animal,
            session,
            new ClinicSettings
            {
                PreferredExportTargetKey = ClinicExportTargets.PdfAttachmentNote,
                PdfAttachmentExportLabel = "PIMS attachment note",
                PdfAttachmentExportNoteTitle = "Post-op attachment note",
                PdfAttachmentInstruction = "Attach the anesthesia PDF to the surgery record."
            },
            buckets,
            Array.Empty<SessionAlert>(),
            completion);

        Assert.Contains("Post-op attachment note", summary);
        Assert.Contains("Primary record: Attach the anesthesia PDF to the surgery record.", summary);
        Assert.Contains("Target workflow: PIMS attachment note", summary);
    }

    [Fact]
    public void Build_IncludesPilotWorkflowGuidance_WhenConfigured()
    {
        var animal = new Animal { Name = "Maple", Species = "Canine" };
        var session = new AnesthesiaSession
        {
            Procedure = "Mass removal",
            SessionStartTime = new DateTime(2026, 6, 15, 11, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 11, 35, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 11, 30, 0),
                HeartRate = 94,
                Map = 70,
                Spo2 = 99
            }
        };
        var settings = new ClinicSettings
        {
            CurrentSoftwareName = "Avimark",
            ChartCopyDestination = "Surgery note field",
            PdfAttachmentDestination = "Patient documents tab",
            PreferredNoteWording = "Use concise post-op wording with anesthesia stability called out.",
            PilotWorkflowNotes = "Paste the note before recovery.\nAttach the PDF before closing the chart.",
            ChartFieldLabelsJson = "{\"HeartRate\":\"Pulse\",\"Map\":\"Mean BP\"}",
            RequiredCompletionFieldKeysCsv = "HeartRate,Map"
        };
        var completion = _completionEvaluator.Evaluate(
            session,
            buckets,
            settings,
            Array.Empty<VoiceEntryLog>(),
            new DateTime(2026, 6, 15, 11, 34, 0));

        var summary = _handoffSummaryService.Build(
            animal,
            session,
            settings,
            buckets,
            Array.Empty<SessionAlert>(),
            completion);

        Assert.Contains("Pilot workflow:", summary);
        Assert.Contains("Current software: Avimark", summary);
        Assert.Contains("Copy chart note into: Surgery note field", summary);
        Assert.Contains("Attach PDF in: Patient documents tab", summary);
        Assert.Contains("Preferred wording: Use concise post-op wording with anesthesia stability called out.", summary);
        Assert.Contains("Required handoff fields: Pulse, Mean BP", summary);
        Assert.Contains("Workflow note: Paste the note before recovery.", summary);
        Assert.Contains("Workflow note: Attach the PDF before closing the chart.", summary);
    }

    [Fact]
    public void BuildBucketCsv_UsesConfiguredLabelsAndOrder()
    {
        var animal = new Animal { Name = "Ember", Species = "Canine" };
        var session = new AnesthesiaSession
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Procedure = "Dental",
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 9, 45, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 5, 0),
                BucketEndTime = new DateTime(2026, 6, 15, 9, 10, 0),
                HeartRate = 95,
                Map = 71,
                RespiratoryRate = 16,
                Spo2 = 98,
                Notes = "stable"
            }
        };
        var settings = new ClinicSettings
        {
            ChartFieldLabelsJson = "{\"Map\":\"Mean BP\",\"HeartRate\":\"Pulse\"}",
            ChartFieldOrderCsv = "Map,HeartRate,Spo2"
        };

        var csv = _structuredExportService.BuildBucketCsv(animal, session, settings, buckets);

        Assert.Contains("\"SessionId\",\"PatientName\",\"Species\",\"Procedure\"", csv);
        Assert.Contains("\"Mean BP\",\"Pulse\",\"SpO2\"", csv);
        Assert.Contains("\"11111111-1111-1111-1111-111111111111\"", csv);
        Assert.Contains("\"Ember\"", csv);
        Assert.Contains("\"Dental\"", csv);
        Assert.Contains("\"71\",\"95\",\"98\"", csv);
        Assert.Contains("\"stable\"", csv);
    }

    [Fact]
    public void BuildOptions_UsesCustomClinicSettingsLabels()
    {
        var settings = new ClinicSettings
        {
            ClinicChartExportLabel = "Record note",
            ClinicChartExportNoteTitle = "Chart-ready record note",
            PdfAttachmentExportLabel = "File attachment note",
            PdfAttachmentExportNoteTitle = "Attachment-ready note"
        };

        var options = ClinicExportTargets.BuildOptions(settings);

        Assert.Contains(options, x => x.Key == ClinicExportTargets.ClinicChartNote && x.Label == "Record note" && x.NoteTitle == "Chart-ready record note");
        Assert.Contains(options, x => x.Key == ClinicExportTargets.PdfAttachmentNote && x.Label == "File attachment note" && x.NoteTitle == "Attachment-ready note");
    }
}
