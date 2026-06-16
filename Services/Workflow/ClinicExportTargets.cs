using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public static class ClinicExportTargets
{
    public const string ClinicChartNote = "ClinicChartNote";
    public const string PdfAttachmentNote = "PdfAttachmentNote";

    public static IReadOnlyList<ClinicExportTargetOption> BuildOptions(ClinicSettings settings) =>
    [
        new(
            ClinicChartNote,
            ValueOrDefault(settings.ClinicChartExportLabel, "Clinic chart note"),
            ValueOrDefault(settings.ClinicChartExportNoteTitle, "Anesthesia chart note"),
            ValueOrDefault(settings.ClinicChartExportDescription, "Paste a complete anesthesia note directly into the patient chart.")),
        new(
            PdfAttachmentNote,
            ValueOrDefault(settings.PdfAttachmentExportLabel, "PDF attachment note"),
            ValueOrDefault(settings.PdfAttachmentExportNoteTitle, "Anesthesia PDF attachment note"),
            ValueOrDefault(settings.PdfAttachmentExportDescription, "Attach the PDF record and paste a short note that points staff to it."),
            ValueOrDefault(settings.PdfAttachmentInstruction, "Attach the exported anesthesia PDF record to the patient chart."))
    ];

    public static ClinicExportTargetOption Resolve(ClinicSettings settings, string? key) =>
        BuildOptions(settings).FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? BuildOptions(settings)[0];

    private static string ValueOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
