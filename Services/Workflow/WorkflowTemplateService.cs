using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;

namespace VetAnesthesiaApp.Services.Workflow;

public class WorkflowTemplateService : IWorkflowTemplateService
{
    private readonly IAnesthesiaRepository _repository;

    public WorkflowTemplateService(IAnesthesiaRepository repository)
    {
        _repository = repository;
    }

    public Task<List<WorkflowTemplate>> GetTemplatesAsync() =>
        _repository.GetWorkflowTemplatesAsync();

    public async Task<WorkflowTemplate> SaveTemplateFromSettingsAsync(string name, string? description, ClinicSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Template name is required.");

        var template = new WorkflowTemplate
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ChartFieldLabelsJson = settings.ChartFieldLabelsJson,
            ChartFieldOrderCsv = settings.ChartFieldOrderCsv,
            RequiredCompletionFieldKeysCsv = settings.RequiredCompletionFieldKeysCsv,
            PreferredExportTargetKey = settings.PreferredExportTargetKey,
            PdfDocumentTitle = settings.PdfDocumentTitle,
            ClinicChartExportLabel = settings.ClinicChartExportLabel,
            ClinicChartExportNoteTitle = settings.ClinicChartExportNoteTitle,
            ClinicChartExportDescription = settings.ClinicChartExportDescription,
            PdfAttachmentExportLabel = settings.PdfAttachmentExportLabel,
            PdfAttachmentExportNoteTitle = settings.PdfAttachmentExportNoteTitle,
            PdfAttachmentExportDescription = settings.PdfAttachmentExportDescription,
            PdfAttachmentInstruction = settings.PdfAttachmentInstruction,
            CsvExportLabel = settings.CsvExportLabel,
            CsvShareTitle = settings.CsvShareTitle,
            CurrentSoftwareName = settings.CurrentSoftwareName,
            ChartCopyDestination = settings.ChartCopyDestination,
            PdfAttachmentDestination = settings.PdfAttachmentDestination,
            PreferredNoteWording = settings.PreferredNoteWording,
            PilotWorkflowNotes = settings.PilotWorkflowNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveWorkflowTemplateAsync(template);
        return template;
    }

    public void ApplyTemplateToSettings(WorkflowTemplate template, ClinicSettings settings)
    {
        settings.ChartFieldLabelsJson = template.ChartFieldLabelsJson;
        settings.ChartFieldOrderCsv = template.ChartFieldOrderCsv;
        settings.RequiredCompletionFieldKeysCsv = template.RequiredCompletionFieldKeysCsv;
        settings.PreferredExportTargetKey = string.IsNullOrWhiteSpace(template.PreferredExportTargetKey)
            ? ClinicExportTargets.ClinicChartNote
            : template.PreferredExportTargetKey;
        settings.PdfDocumentTitle = template.PdfDocumentTitle;
        settings.ClinicChartExportLabel = template.ClinicChartExportLabel;
        settings.ClinicChartExportNoteTitle = template.ClinicChartExportNoteTitle;
        settings.ClinicChartExportDescription = template.ClinicChartExportDescription;
        settings.PdfAttachmentExportLabel = template.PdfAttachmentExportLabel;
        settings.PdfAttachmentExportNoteTitle = template.PdfAttachmentExportNoteTitle;
        settings.PdfAttachmentExportDescription = template.PdfAttachmentExportDescription;
        settings.PdfAttachmentInstruction = template.PdfAttachmentInstruction;
        settings.CsvExportLabel = template.CsvExportLabel;
        settings.CsvShareTitle = template.CsvShareTitle;
        settings.CurrentSoftwareName = template.CurrentSoftwareName;
        settings.ChartCopyDestination = template.ChartCopyDestination;
        settings.PdfAttachmentDestination = template.PdfAttachmentDestination;
        settings.PreferredNoteWording = template.PreferredNoteWording;
        settings.PilotWorkflowNotes = template.PilotWorkflowNotes;
    }

    public Task DeleteTemplateAsync(Guid templateId) =>
        _repository.DeleteWorkflowTemplateAsync(templateId);
}
