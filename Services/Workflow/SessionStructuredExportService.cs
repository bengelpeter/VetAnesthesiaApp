using System.Text;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public class SessionStructuredExportService : ISessionStructuredExportService
{
    private readonly IChartConfigurationService _chartConfigurationService;

    public SessionStructuredExportService(IChartConfigurationService chartConfigurationService)
    {
        _chartConfigurationService = chartConfigurationService;
    }

    public string BuildBucketCsv(Animal? animal, AnesthesiaSession session, ClinicSettings settings, IReadOnlyList<AnesthesiaBucket> buckets)
    {
        var csv = new StringBuilder();
        var fields = _chartConfigurationService.GetConfiguredFields(settings);
        var headers = new List<string>
        {
            "SessionId",
            "PatientName",
            "Species",
            "Procedure",
            "SessionStart",
            "SessionEnd",
            "BucketStart",
            "BucketEnd"
        };
        headers.AddRange(fields.Select(x => x.Label));
        headers.Add("Notes");
        csv.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (var bucket in buckets.OrderBy(x => x.BucketStartTime))
        {
            var values = new List<string>
            {
                Escape(session.Id),
                Escape(animal?.Name),
                Escape(animal?.Species),
                Escape(session.Procedure),
                Escape(session.SessionStartTime),
                Escape(session.SessionEndTime),
                Escape(bucket.BucketStartTime),
                Escape(bucket.BucketEndTime)
            };

            values.AddRange(fields.Select(field => Escape(FormatFieldValue(field.ValueSelector(bucket), field.IsWholeNumber))));
            values.Add(Escape(bucket.Notes));
            csv.AppendLine(string.Join(",", values));
        }

        return csv.ToString().TrimEnd();
    }

    public async Task<string> ExportBucketCsvAsync(Animal? animal, AnesthesiaSession session, ClinicSettings settings, IReadOnlyList<AnesthesiaBucket> buckets)
    {
        var exportDirectory = GetExportDirectory();
        Directory.CreateDirectory(exportDirectory);

        var fileName = $"anesthesia-session-{session.Id}-buckets.csv";
        var filePath = Path.Combine(exportDirectory, fileName);
        var csv = BuildBucketCsv(animal, session, settings, buckets);

        await File.WriteAllTextAsync(filePath, csv);
        return filePath;
    }

    private static string FormatFieldValue(decimal? value, bool isWholeNumber)
    {
        if (!value.HasValue)
            return string.Empty;

        return isWholeNumber
            ? ((int)value.Value).ToString()
            : (value.Value % 1 == 0 ? value.Value.ToString("0") : value.Value.ToString("0.##"));
    }

    private static string Escape(object? value)
    {
        if (value is null)
            return string.Empty;

        var text = value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            _ => value.ToString() ?? string.Empty
        };

        text = text.Replace("\"", "\"\"");
        return $"\"{text}\"";
    }

    private static string GetExportDirectory()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        return Microsoft.Maui.Storage.FileSystem.CacheDirectory;
#else
        return Path.GetTempPath();
#endif
    }
}
