using SQLite;

namespace VetAnesthesiaApp.Models;

public class ClinicSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public int MonitoringIntervalMinutes { get; set; } = 5;
    public int MissingVitalsAlertAfterMinutes { get; set; } = 10;
    public int Spo2LowThreshold { get; set; } = 95;
    public int MapLowThreshold { get; set; } = 60;
    public decimal TemperatureDropAlertDelta { get; set; } = 1.0m;
    public bool EnableAlerts { get; set; } = true;
    public string ClinicName { get; set; } = "";
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
}
