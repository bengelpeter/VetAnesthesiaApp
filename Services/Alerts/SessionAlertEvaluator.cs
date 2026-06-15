using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Alerts;

public class SessionAlertEvaluator : ISessionAlertEvaluator
{
    public List<SessionAlert> Evaluate(IReadOnlyList<AnesthesiaBucket> buckets, ClinicSettings settings, DateTime now)
    {
        var alerts = new List<SessionAlert>();

        if (!settings.EnableAlerts || buckets.Count == 0)
            return alerts;

        var latestBucket = buckets[^1];

        if (!latestBucket.Spo2.HasValue)
        {
            alerts.Add(new SessionAlert(
                VetAlertLevel.Warning,
                "Latest bucket is missing SpO2."));
        }

        if (latestBucket.Spo2.HasValue && latestBucket.Spo2.Value < settings.Spo2LowThreshold)
        {
            alerts.Add(new SessionAlert(
                VetAlertLevel.Critical,
                $"SpO2 is {latestBucket.Spo2.Value} and below the clinic threshold of {settings.Spo2LowThreshold}."));
        }

        if (!latestBucket.Map.HasValue && !(latestBucket.SystolicBp.HasValue && latestBucket.DiastolicBp.HasValue))
        {
            alerts.Add(new SessionAlert(
                VetAlertLevel.Warning,
                "Latest bucket is missing blood pressure or MAP."));
        }

        var recentMapBuckets = buckets
            .Where(x => x.Map.HasValue)
            .TakeLast(2)
            .ToList();

        if (recentMapBuckets.Count == 2 &&
            recentMapBuckets.All(x => x.Map!.Value < settings.MapLowThreshold))
        {
            alerts.Add(new SessionAlert(
                VetAlertLevel.Warning,
                $"MAP has remained below the clinic threshold of {settings.MapLowThreshold} for two consecutive readings."));
        }

        var minutesSinceLatestBucket = (now - latestBucket.BucketStartTime).TotalMinutes;
        if (minutesSinceLatestBucket >= settings.MissingVitalsAlertAfterMinutes)
        {
            alerts.Add(new SessionAlert(
                VetAlertLevel.Warning,
                $"No new vitals have been recorded for {Math.Floor(minutesSinceLatestBucket)} minutes."));
        }

        var latestTemperatureBucket = buckets
            .Where(x => x.Temperature.HasValue)
            .LastOrDefault();

        if (latestTemperatureBucket?.Temperature is not null)
        {
            var windowStart = latestTemperatureBucket.BucketStartTime.AddMinutes(-20);
            var baselineTemperatureBucket = buckets
                .Where(x => x.Temperature.HasValue && x.BucketStartTime >= windowStart && x.BucketStartTime <= latestTemperatureBucket.BucketStartTime)
                .FirstOrDefault();

            if (baselineTemperatureBucket?.Temperature is not null)
            {
                var drop = baselineTemperatureBucket.Temperature.Value - latestTemperatureBucket.Temperature.Value;
                if (drop >= settings.TemperatureDropAlertDelta)
                {
                    alerts.Add(new SessionAlert(
                        VetAlertLevel.Warning,
                        $"Temperature has fallen {drop:0.##}F over the last 20 minutes."));
                }
            }
        }

        return alerts;
    }
}
