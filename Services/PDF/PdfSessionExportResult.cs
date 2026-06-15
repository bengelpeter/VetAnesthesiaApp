namespace VetAnesthesiaApp.Services.Pdf;

public sealed record PdfSessionExportResult(
    string FilePath,
    bool ShareRequested,
    string? ShareErrorMessage);
