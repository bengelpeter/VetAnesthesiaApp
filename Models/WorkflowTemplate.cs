using SQLite;

namespace VetAnesthesiaApp.Models;

public class WorkflowTemplate
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public string ChartFieldLabelsJson { get; set; } = "";
    public string ChartFieldOrderCsv { get; set; } = "";
    public string RequiredCompletionFieldKeysCsv { get; set; } = "";

    public string PreferredExportTargetKey { get; set; } = "ClinicChartNote";
    public string PdfDocumentTitle { get; set; } = "Anesthesia Record";
    public string ClinicChartExportLabel { get; set; } = "Clinic chart note";
    public string ClinicChartExportNoteTitle { get; set; } = "Anesthesia chart note";
    public string ClinicChartExportDescription { get; set; } = "Paste a complete anesthesia note directly into the patient chart.";
    public string PdfAttachmentExportLabel { get; set; } = "PDF attachment note";
    public string PdfAttachmentExportNoteTitle { get; set; } = "Anesthesia PDF attachment note";
    public string PdfAttachmentExportDescription { get; set; } = "Attach the PDF record and paste a short note that points staff to it.";
    public string PdfAttachmentInstruction { get; set; } = "Attach the exported anesthesia PDF record to the patient chart.";
    public string CsvExportLabel { get; set; } = "Bucket CSV";
    public string CsvShareTitle { get; set; } = "Share bucket CSV";
    public string CurrentSoftwareName { get; set; } = "";
    public string ChartCopyDestination { get; set; } = "";
    public string PdfAttachmentDestination { get; set; } = "";
    public string PreferredNoteWording { get; set; } = "";
    public string PilotWorkflowNotes { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
