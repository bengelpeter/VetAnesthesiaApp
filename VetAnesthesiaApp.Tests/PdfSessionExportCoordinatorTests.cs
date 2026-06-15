using VetAnesthesiaApp.Services.Pdf;

namespace VetAnesthesiaApp.Tests;

public class PdfSessionExportCoordinatorTests
{
    [Fact]
    public async Task ReturnsShareRequestedWhenExportAndShareSucceed()
    {
        var sessionId = Guid.NewGuid();
        var exportService = new StubPdfExportService("C:\\temp\\record.pdf");
        var shareService = new StubFileShareService();
        var coordinator = new PdfSessionExportCoordinator(exportService, shareService);

        var result = await coordinator.ExportAndShareAsync(sessionId);

        Assert.True(result.ShareRequested);
        Assert.Null(result.ShareErrorMessage);
        Assert.Equal("C:\\temp\\record.pdf", result.FilePath);
        Assert.Equal(sessionId, exportService.RequestedSessionId);
        Assert.Equal("C:\\temp\\record.pdf", shareService.SharedPath);
        Assert.Equal("Share anesthesia record", shareService.SharedTitle);
    }

    [Fact]
    public async Task ReturnsSavedFileWhenShareFailsAfterSuccessfulExport()
    {
        var coordinator = new PdfSessionExportCoordinator(
            new StubPdfExportService("C:\\temp\\record.pdf"),
            new StubFileShareService(new InvalidOperationException("No apps can share this file.")));

        var result = await coordinator.ExportAndShareAsync(Guid.NewGuid());

        Assert.False(result.ShareRequested);
        Assert.Equal("C:\\temp\\record.pdf", result.FilePath);
        Assert.Equal("No apps can share this file.", result.ShareErrorMessage);
    }

    [Fact]
    public async Task PropagatesExportFailureWithoutAttemptingShare()
    {
        var exportService = new StubPdfExportService(exceptionToThrow: new InvalidOperationException("Session not found."));
        var shareService = new StubFileShareService();
        var coordinator = new PdfSessionExportCoordinator(exportService, shareService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExportAndShareAsync(Guid.NewGuid()));

        Assert.Equal("Session not found.", ex.Message);
        Assert.Null(shareService.SharedPath);
    }

    private sealed class StubPdfExportService : IPdfExportService
    {
        private readonly string _filePath;
        private readonly Exception? _exceptionToThrow;

        public StubPdfExportService(string filePath = "", Exception? exceptionToThrow = null)
        {
            _filePath = filePath;
            _exceptionToThrow = exceptionToThrow;
        }

        public Guid RequestedSessionId { get; private set; }

        public Task<string> ExportSessionPdfAsync(Guid sessionId)
        {
            RequestedSessionId = sessionId;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            return Task.FromResult(_filePath);
        }
    }

    private sealed class StubFileShareService : IFileShareService
    {
        private readonly Exception? _exceptionToThrow;

        public StubFileShareService(Exception? exceptionToThrow = null)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public string? SharedPath { get; private set; }
        public string? SharedTitle { get; private set; }

        public Task ShareFileAsync(string filePath, string title)
        {
            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            SharedPath = filePath;
            SharedTitle = title;
            return Task.CompletedTask;
        }
    }
}
