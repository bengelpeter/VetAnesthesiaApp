using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface ISessionStructuredExportService
{
    string BuildBucketCsv(Animal? animal, AnesthesiaSession session, ClinicSettings settings, IReadOnlyList<AnesthesiaBucket> buckets);
    Task<string> ExportBucketCsvAsync(Animal? animal, AnesthesiaSession session, ClinicSettings settings, IReadOnlyList<AnesthesiaBucket> buckets);
}
