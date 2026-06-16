namespace VetAnesthesiaApp.Services.Workflow;

public static class ClinicExportTargets
{
    public const string ClinicChartNote = "ClinicChartNote";
    public const string PdfAttachmentNote = "PdfAttachmentNote";

    public static IReadOnlyList<ClinicExportTargetOption> All { get; } =
    [
        new(
            ClinicChartNote,
            "Clinic chart note",
            "Paste a complete anesthesia note directly into the patient chart."),
        new(
            PdfAttachmentNote,
            "PDF attachment note",
            "Attach the PDF record and paste a short note that points staff to it.")
    ];

    public static ClinicExportTargetOption Resolve(string? key) =>
        All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? All[0];
}
