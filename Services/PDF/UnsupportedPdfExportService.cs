namespace VetAnesthesiaApp.Services.Pdf;

public class UnsupportedPdfExportService : IPdfExportService
{
    public Task<string> ExportSessionPdfAsync(Guid sessionId)
    {
        throw new PlatformNotSupportedException(
            "PDF export is currently supported on Android and Windows in this build.");
    }
}
