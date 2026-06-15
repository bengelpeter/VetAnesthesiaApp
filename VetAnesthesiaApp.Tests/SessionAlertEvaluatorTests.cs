using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Alerts;

namespace VetAnesthesiaApp.Tests;

public class SessionAlertEvaluatorTests
{
    private readonly SessionAlertEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsSpo2Alert_WhenLatestReadingIsBelowThreshold()
    {
        var settings = new ClinicSettings { Spo2LowThreshold = 95 };
        var buckets = new[]
        {
            new AnesthesiaBucket
            {
                BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0),
                Spo2 = 92,
                Map = 70
            }
        };

        var alerts = _evaluator.Evaluate(buckets, settings, new DateTime(2026, 6, 15, 9, 1, 0));

        Assert.Contains(alerts, x => x.Message.Contains("SpO2 is 92"));
    }

    [Fact]
    public void Evaluate_ReturnsConsecutiveMapAlert_WhenTwoRecentReadingsAreLow()
    {
        var settings = new ClinicSettings { MapLowThreshold = 60 };
        var buckets = new[]
        {
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0), Map = 58, Spo2 = 97 },
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 5, 0), Map = 55, Spo2 = 96 }
        };

        var alerts = _evaluator.Evaluate(buckets, settings, new DateTime(2026, 6, 15, 9, 6, 0));

        Assert.Contains(alerts, x => x.Message.Contains("two consecutive readings"));
    }

    [Fact]
    public void Evaluate_ReturnsMissingVitalsAlert_WhenLatestBucketIsStale()
    {
        var settings = new ClinicSettings { MissingVitalsAlertAfterMinutes = 10 };
        var buckets = new[]
        {
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0), Spo2 = 98, Map = 70 }
        };

        var alerts = _evaluator.Evaluate(buckets, settings, new DateTime(2026, 6, 15, 9, 15, 0));

        Assert.Contains(alerts, x => x.Message.Contains("No new vitals have been recorded"));
    }

    [Fact]
    public void Evaluate_ReturnsTemperatureDropAlert_WhenTemperatureFallsAcrossWindow()
    {
        var settings = new ClinicSettings { TemperatureDropAlertDelta = 1.0m };
        var buckets = new[]
        {
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0), Temperature = 100.2m, Spo2 = 98, Map = 72 },
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 15, 0), Temperature = 98.9m, Spo2 = 97, Map = 68 }
        };

        var alerts = _evaluator.Evaluate(buckets, settings, new DateTime(2026, 6, 15, 9, 16, 0));

        Assert.Contains(alerts, x => x.Message.Contains("Temperature has fallen"));
    }

    [Fact]
    public void Evaluate_ReturnsNoAlerts_WhenAlertsAreDisabled()
    {
        var settings = new ClinicSettings { EnableAlerts = false, Spo2LowThreshold = 95 };
        var buckets = new[]
        {
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0), Spo2 = 80 }
        };

        var alerts = _evaluator.Evaluate(buckets, settings, new DateTime(2026, 6, 15, 9, 15, 0));

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_ReturnsMissingSpo2Alert_WhenLatestBucketHasNoSpo2()
    {
        var buckets = new[]
        {
            new AnesthesiaBucket { BucketStartTime = new DateTime(2026, 6, 15, 9, 0, 0), Map = 70 }
        };

        var alerts = _evaluator.Evaluate(buckets, new ClinicSettings(), new DateTime(2026, 6, 15, 9, 1, 0));

        Assert.Contains(alerts, x => x.Message.Contains("missing SpO2"));
    }
}
