using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface ISessionStructuredExportService
{
    string BuildBucketCsv(Animal? animal, AnesthesiaSession session, IReadOnlyList<AnesthesiaBucket> buckets);
    Task<string> ExportBucketCsvAsync(Animal? animal, AnesthesiaSession session, IReadOnlyList<AnesthesiaBucket> buckets);
}
