using System.Globalization;
using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Buckets;

public static class AnesthesiaFieldValidator
{
    private static readonly IReadOnlyDictionary<string, AnesthesiaFieldRule> Rules =
        new Dictionary<string, AnesthesiaFieldRule>
        {
            [AnesthesiaFieldKeys.IsoPercent] = new(AnesthesiaFieldKeys.IsoPercent, "ISO %", 0m, 10m, true),
            [AnesthesiaFieldKeys.OxygenFlowRate] = new(AnesthesiaFieldKeys.OxygenFlowRate, "O2 flow", 0m, 15m, true),
            [AnesthesiaFieldKeys.Etco2] = new(AnesthesiaFieldKeys.Etco2, "ETCO2", 0m, 120m, true),
            [AnesthesiaFieldKeys.Spo2] = new(AnesthesiaFieldKeys.Spo2, "SpO2", 0m, 100m, false),
            [AnesthesiaFieldKeys.Temperature] = new(AnesthesiaFieldKeys.Temperature, "Temperature", 20m, 120m, true),
            [AnesthesiaFieldKeys.HeartRate] = new(AnesthesiaFieldKeys.HeartRate, "Heart rate", 0m, 400m, false),
            [AnesthesiaFieldKeys.RespiratoryRate] = new(AnesthesiaFieldKeys.RespiratoryRate, "Respiratory rate", 0m, 120m, false),
            [AnesthesiaFieldKeys.SystolicBp] = new(AnesthesiaFieldKeys.SystolicBp, "Systolic BP", 0m, 300m, false),
            [AnesthesiaFieldKeys.DiastolicBp] = new(AnesthesiaFieldKeys.DiastolicBp, "Diastolic BP", 0m, 250m, false),
            [AnesthesiaFieldKeys.Map] = new(AnesthesiaFieldKeys.Map, "MAP", 0m, 250m, false)
        };

    public static decimal ValidateAndNormalizeValue(string fieldKey, decimal value)
    {
        var rule = GetRule(fieldKey);

        if (!rule.AllowsDecimal && value != decimal.Truncate(value))
            throw new InvalidOperationException($"{rule.Label} must be a whole number.");

        if (value < rule.Min || value > rule.Max)
            throw new InvalidOperationException($"{rule.Label} must be between {Format(rule.Min)} and {Format(rule.Max)}.");

        return rule.AllowsDecimal
            ? decimal.Round(value, 2, MidpointRounding.AwayFromZero)
            : decimal.Truncate(value);
    }

    public static string NormalizeNote(string noteText)
    {
        var normalized = noteText?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Note text cannot be empty.");

        return normalized;
    }

    public static void ValidateBucketStartTime(DateTime bucketStartTime, AnesthesiaBucket? activeBucket)
    {
        if (activeBucket is null)
            return;

        if (bucketStartTime < activeBucket.BucketStartTime)
            throw new InvalidOperationException("New bucket time cannot be earlier than the active bucket start time.");
    }

    private static AnesthesiaFieldRule GetRule(string fieldKey)
    {
        if (Rules.TryGetValue(fieldKey, out var rule))
            return rule;

        throw new InvalidOperationException($"Unsupported field key: {fieldKey}");
    }

    private static string Format(decimal value)
    {
        return value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

public sealed record AnesthesiaFieldRule(
    string FieldKey,
    string Label,
    decimal Min,
    decimal Max,
    bool AllowsDecimal);
