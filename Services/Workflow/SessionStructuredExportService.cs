using System.Text;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public class SessionStructuredExportService : ISessionStructuredExportService
{
    public string BuildBucketCsv(Animal? animal, AnesthesiaSession session, IReadOnlyList<AnesthesiaBucket> buckets)
    {
        var csv = new StringBuilder();
        csv.AppendLine("SessionId,PatientName,Species,Procedure,SessionStart,SessionEnd,BucketStart,BucketEnd,ISO,O2Flow,ETCO2,SpO2,Temp,HeartRate,RespiratoryRate,Systolic,Diastolic,MAP,Notes");

        foreach (var bucket in buckets.OrderBy(x => x.BucketStartTime))
        {
            csv.AppendLine(string.Join(",",
                Escape(session.Id),
                Escape(animal?.Name),
                Escape(animal?.Species),
                Escape(session.Procedure),
                Escape(session.SessionStartTime),
                Escape(session.SessionEndTime),
                Escape(bucket.BucketStartTime),
                Escape(bucket.BucketEndTime),
                Escape(bucket.IsoPercent),
                Escape(bucket.OxygenFlowRate),
                Escape(bucket.Etco2),
                Escape(bucket.Spo2),
                Escape(bucket.Temperature),
                Escape(bucket.HeartRate),
                Escape(bucket.RespiratoryRate),
                Escape(bucket.SystolicBp),
                Escape(bucket.DiastolicBp),
                Escape(bucket.Map),
                Escape(bucket.Notes)));
        }

        return csv.ToString().TrimEnd();
    }

    public async Task<string> ExportBucketCsvAsync(Animal? animal, AnesthesiaSession session, IReadOnlyList<AnesthesiaBucket> buckets)
    {
        var exportDirectory = GetExportDirectory();
        Directory.CreateDirectory(exportDirectory);

        var fileName = $"anesthesia-session-{session.Id}-buckets.csv";
        var filePath = Path.Combine(exportDirectory, fileName);
        var csv = BuildBucketCsv(animal, session, buckets);

        await File.WriteAllTextAsync(filePath, csv);
        return filePath;
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
