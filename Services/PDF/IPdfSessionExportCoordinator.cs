namespace VetAnesthesiaApp.Services.Pdf;

public interface IPdfSessionExportCoordinator
{
    Task<PdfSessionExportResult> ExportAndShareAsync(Guid sessionId);
}
