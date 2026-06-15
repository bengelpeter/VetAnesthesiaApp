namespace VetAnesthesiaApp.Services.Pdf;

public class PdfSessionExportCoordinator : IPdfSessionExportCoordinator
{
    private readonly IPdfExportService _pdfExportService;
    private readonly IFileShareService _fileShareService;

    public PdfSessionExportCoordinator(IPdfExportService pdfExportService, IFileShareService fileShareService)
    {
        _pdfExportService = pdfExportService;
        _fileShareService = fileShareService;
    }

    public async Task<PdfSessionExportResult> ExportAndShareAsync(Guid sessionId)
    {
        var filePath = await _pdfExportService.ExportSessionPdfAsync(sessionId);

        try
        {
            await _fileShareService.ShareFileAsync(filePath, "Share anesthesia record");
            return new PdfSessionExportResult(filePath, true, null);
        }
        catch (Exception ex)
        {
            return new PdfSessionExportResult(filePath, false, ex.Message);
        }
    }
}
