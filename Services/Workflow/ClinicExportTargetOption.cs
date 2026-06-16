namespace VetAnesthesiaApp.Services.Workflow;

public sealed record ClinicExportTargetOption(
    string Key,
    string Label,
    string NoteTitle,
    string Description,
    string? PrimaryRecordInstruction = null);
