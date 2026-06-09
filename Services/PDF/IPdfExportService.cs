using System;
using System.Collections.Generic;
using System.Text;

namespace VetAnesthesiaApp.Services.Pdf
{
    public interface IPdfExportService
    {
        Task<string> ExportSessionPdfAsync(Guid sessionId);
    }
}
