using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Workflow;

namespace VetAnesthesiaApp.Tests;

public class WorkflowConfigurationTests
{
    private readonly IChartConfigurationService _chartConfigurationService = new ChartConfigurationService();

    [Fact]
    public void Evaluate_UsesConfiguredRequiredFieldLabels()
    {
        var evaluator = new SessionCompletionEvaluator(_chartConfigurationService);
        var session = new AnesthesiaSession
        {
            Procedure = "Dental",
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0)
        };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 5, 0),
                Spo2 = 98
            }
        };
        var settings = new ClinicSettings
        {
            ChartFieldLabelsJson = "{\"HeartRate\":\"Pulse\",\"Map\":\"Mean BP\"}",
            RequiredCompletionFieldKeysCsv = "HeartRate,Map"
        };

        var summary = evaluator.Evaluate(
            session,
            buckets,
            settings,
            Array.Empty<VoiceEntryLog>(),
            new DateTime(2026, 6, 15, 9, 7, 0));

        var latestVitals = Assert.Single(summary.Items, x => x.Key == "latest-vitals");
        Assert.Contains("Pulse", latestVitals.Message);
        Assert.Contains("Mean BP", latestVitals.Message);
    }

    [Fact]
    public async Task WorkflowTemplateService_SavesAppliesAndDeletesTemplates()
    {
        var repository = new InMemoryAnesthesiaRepository();
        var service = new WorkflowTemplateService(repository);
        var settings = new ClinicSettings
        {
            PreferredExportTargetKey = ClinicExportTargets.PdfAttachmentNote,
            ChartFieldLabelsJson = "{\"HeartRate\":\"Pulse\"}",
            ChartFieldOrderCsv = "HeartRate,Map,Spo2",
            RequiredCompletionFieldKeysCsv = "HeartRate,Map",
            PdfDocumentTitle = "Anesthesia Attachment",
            ClinicChartExportLabel = "SOAP note",
            CsvShareTitle = "Share surgery bucket CSV"
        };

        var template = await service.SaveTemplateFromSettingsAsync("ER handoff", "PDF-first workflow", settings);

        Assert.Equal(ClinicExportTargets.PdfAttachmentNote, template.PreferredExportTargetKey);

        var applied = new ClinicSettings();
        service.ApplyTemplateToSettings(template, applied);

        Assert.Equal(ClinicExportTargets.PdfAttachmentNote, applied.PreferredExportTargetKey);
        Assert.Equal("{\"HeartRate\":\"Pulse\"}", applied.ChartFieldLabelsJson);
        Assert.Equal("HeartRate,Map,Spo2", applied.ChartFieldOrderCsv);
        Assert.Equal("HeartRate,Map", applied.RequiredCompletionFieldKeysCsv);
        Assert.Equal("Anesthesia Attachment", applied.PdfDocumentTitle);
        Assert.Equal("SOAP note", applied.ClinicChartExportLabel);
        Assert.Equal("Share surgery bucket CSV", applied.CsvShareTitle);

        await service.DeleteTemplateAsync(template.Id);

        var templates = await service.GetTemplatesAsync();
        Assert.Empty(templates);
    }

    [Fact]
    public async Task WorkflowTelemetryService_BuildsSessionFragilitySummary()
    {
        var repository = new InMemoryAnesthesiaRepository();
        var service = new WorkflowTelemetryService(repository);
        var session = new AnesthesiaSession
        {
            Id = Guid.NewGuid(),
            SessionStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
            SessionEndTime = new DateTime(2026, 6, 15, 9, 42, 0)
        };

        await repository.SaveSessionAsync(session);
        await repository.SaveVoiceLogAsync(new VoiceEntryLog
        {
            SessionId = session.Id,
            SpokenAt = new DateTime(2026, 6, 15, 9, 5, 0),
            Applied = true,
            MatchedCommandType = "FieldValue"
        });
        await repository.SaveVoiceLogAsync(new VoiceEntryLog
        {
            SessionId = session.Id,
            SpokenAt = new DateTime(2026, 6, 15, 9, 6, 0),
            Applied = false,
            MatchedCommandType = "FieldValue"
        });
        await repository.SaveVoiceLogAsync(new VoiceEntryLog
        {
            SessionId = session.Id,
            SpokenAt = new DateTime(2026, 6, 15, 9, 7, 0),
            Applied = true,
            MatchedCommandType = "Undo"
        });

        await service.RecordManualCorrectionAsync(session.Id, Guid.NewGuid(), "HeartRate", "90 -> 95");
        await service.RecordSessionCompletionChangeAsync(session.Id, true);

        var summary = await service.BuildSummaryAsync(session);

        Assert.Equal(3, summary.VoiceAttempts);
        Assert.Equal(1, summary.FailedVoiceAttempts);
        Assert.Equal(1, summary.UndoUsage);
        Assert.Equal(1, summary.ManualCorrections);
        Assert.Equal(1, summary.CompletionEvents);
        Assert.Equal(TimeSpan.FromMinutes(42), summary.TimeToCompletion);
    }
}
