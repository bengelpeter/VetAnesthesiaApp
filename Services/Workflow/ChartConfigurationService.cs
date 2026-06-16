using System.Text.Json;
using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public class ChartConfigurationService : IChartConfigurationService
{
    private static readonly IReadOnlyList<ChartFieldDefinition> DefaultFields =
    [
        new(AnesthesiaFieldKeys.IsoPercent, "ISO", false, x => x.IsoPercent),
        new(AnesthesiaFieldKeys.OxygenFlowRate, "O2", false, x => x.OxygenFlowRate),
        new(AnesthesiaFieldKeys.Etco2, "ETCO2", false, x => x.Etco2),
        new(AnesthesiaFieldKeys.Spo2, "SpO2", true, x => x.Spo2),
        new(AnesthesiaFieldKeys.Temperature, "Temp", false, x => x.Temperature),
        new(AnesthesiaFieldKeys.HeartRate, "HR", true, x => x.HeartRate),
        new(AnesthesiaFieldKeys.RespiratoryRate, "RR", true, x => x.RespiratoryRate),
        new(AnesthesiaFieldKeys.SystolicBp, "SYS", true, x => x.SystolicBp),
        new(AnesthesiaFieldKeys.DiastolicBp, "DIA", true, x => x.DiastolicBp),
        new(AnesthesiaFieldKeys.Map, "MAP", true, x => x.Map)
    ];

    public IReadOnlyList<ChartFieldDefinition> GetConfiguredFields(ClinicSettings settings)
    {
        var labels = GetLabelMap(settings);
        var order = GetConfiguredOrder(settings);

        return order
            .Select(key => DefaultFields.FirstOrDefault(x => x.Key == key))
            .Where(x => x is not null)
            .Select(x => x!)
            .Select(x => x with { Label = labels.GetValueOrDefault(x.Key, x.Label) })
            .ToList();
    }

    public IReadOnlyCollection<string> GetRequiredCompletionFieldKeys(ClinicSettings settings)
    {
        var raw = settings.RequiredCompletionFieldKeysCsv ?? string.Empty;
        var keys = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => DefaultFields.Any(field => field.Key == x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return keys.Count == 0
            ? DefaultFields
                .Where(x => x.Key is AnesthesiaFieldKeys.HeartRate or AnesthesiaFieldKeys.RespiratoryRate or AnesthesiaFieldKeys.Spo2 or AnesthesiaFieldKeys.Etco2 or AnesthesiaFieldKeys.Temperature or AnesthesiaFieldKeys.Map)
                .Select(x => x.Key)
                .ToList()
            : keys;
    }

    public Dictionary<string, string> GetLabelMap(ClinicSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ChartFieldLabelsJson))
        {
            return DefaultFields.ToDictionary(x => x.Key, x => x.Label, StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var configured = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.ChartFieldLabelsJson)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in DefaultFields)
            {
                if (!configured.TryGetValue(field.Key, out var label) || string.IsNullOrWhiteSpace(label))
                {
                    configured[field.Key] = field.Label;
                }
            }

            return new Dictionary<string, string>(configured, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return DefaultFields.ToDictionary(x => x.Key, x => x.Label, StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<string> GetConfiguredOrder(ClinicSettings settings)
    {
        var configured = (settings.ChartFieldOrderCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => DefaultFields.Any(field => field.Key == x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var field in DefaultFields)
        {
            if (!configured.Contains(field.Key, StringComparer.OrdinalIgnoreCase))
            {
                configured.Add(field.Key);
            }
        }

        return configured;
    }
}
