using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;

namespace VetAnesthesiaApp.Services.Buckets;

public class BucketService : IBucketService
{
    private readonly IAnesthesiaRepository _repository;

    public BucketService(IAnesthesiaRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnesthesiaBucket> GetOrCreateActiveBucketAsync(Guid sessionId, DateTime now)
    {
        var active = await GetActiveBucketAsync(sessionId);
        if (active is not null)
            return active;

        return await StartNewBucketAsync(sessionId, now);
    }

    public async Task<AnesthesiaBucket?> GetActiveBucketAsync(Guid sessionId)
    {
        var session = await _repository.GetSessionAsync(sessionId);
        if (session?.ActiveBucketId is null)
            return null;

        return await _repository.GetBucketAsync(session.ActiveBucketId.Value);
    }

    public async Task<AnesthesiaBucket> StartNewBucketAsync(Guid sessionId, DateTime bucketStartTime)
    {
        var session = await _repository.GetSessionAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        var current = await GetActiveBucketAsync(sessionId);
        AnesthesiaFieldValidator.ValidateBucketStartTime(bucketStartTime, current);

        if (current is not null && current.BucketEndTime is null)
        {
            current.BucketEndTime = bucketStartTime;
            await _repository.SaveBucketAsync(current);
        }

        var newBucket = new AnesthesiaBucket
        {
            SessionId = sessionId,
            BucketStartTime = bucketStartTime
        };

        await _repository.SaveBucketAsync(newBucket);

        session.ActiveBucketId = newBucket.Id;
        await _repository.SaveSessionAsync(session);

        return newBucket;
    }
    public async Task ApplyFieldValueAsync(Guid bucketId, string fieldKey, decimal value)
    {
        var bucket = await _repository.GetBucketAsync(bucketId)
            ?? throw new InvalidOperationException("Bucket not found.");
        var normalizedValue = AnesthesiaFieldValidator.ValidateAndNormalizeValue(fieldKey, value);

        switch (fieldKey)
        {
            case AnesthesiaFieldKeys.IsoPercent:
                bucket.IsoPercent = normalizedValue; break;
            case AnesthesiaFieldKeys.OxygenFlowRate:
                bucket.OxygenFlowRate = normalizedValue; break;
            case AnesthesiaFieldKeys.Etco2:
                bucket.Etco2 = normalizedValue; break;
            case AnesthesiaFieldKeys.Spo2:
                bucket.Spo2 = (int)normalizedValue; break;
            case AnesthesiaFieldKeys.Temperature:
                bucket.Temperature = normalizedValue; break;
            case AnesthesiaFieldKeys.HeartRate:
                bucket.HeartRate = (int)normalizedValue; break;
            case AnesthesiaFieldKeys.RespiratoryRate:
                bucket.RespiratoryRate = (int)normalizedValue; break;
            case AnesthesiaFieldKeys.SystolicBp:
                bucket.SystolicBp = (int)normalizedValue; break;
            case AnesthesiaFieldKeys.DiastolicBp:
                bucket.DiastolicBp = (int)normalizedValue; break;
            case AnesthesiaFieldKeys.Map:
                bucket.Map = (int)normalizedValue; break;
            default:
                throw new InvalidOperationException($"Unsupported field key: {fieldKey}");
        }

        await _repository.SaveBucketAsync(bucket);
    }

    public async Task ClearFieldAsync(Guid bucketId, string fieldKey)
    {
        var bucket = await _repository.GetBucketAsync(bucketId)
            ?? throw new InvalidOperationException("Bucket not found.");

        switch (fieldKey)
        {
            case AnesthesiaFieldKeys.IsoPercent: bucket.IsoPercent = null; break;
            case AnesthesiaFieldKeys.OxygenFlowRate: bucket.OxygenFlowRate = null; break;
            case AnesthesiaFieldKeys.Etco2: bucket.Etco2 = null; break;
            case AnesthesiaFieldKeys.Spo2: bucket.Spo2 = null; break;
            case AnesthesiaFieldKeys.Temperature: bucket.Temperature = null; break;
            case AnesthesiaFieldKeys.HeartRate: bucket.HeartRate = null; break;
            case AnesthesiaFieldKeys.RespiratoryRate: bucket.RespiratoryRate = null; break;
            case AnesthesiaFieldKeys.SystolicBp: bucket.SystolicBp = null; break;
            case AnesthesiaFieldKeys.DiastolicBp: bucket.DiastolicBp = null; break;
            case AnesthesiaFieldKeys.Map: bucket.Map = null; break;
            default:
                throw new InvalidOperationException($"Unsupported field key: {fieldKey}");
        }

        await _repository.SaveBucketAsync(bucket);
    }

    public async Task AppendNoteAsync(Guid bucketId, string noteText)
    {
        var bucket = await _repository.GetBucketAsync(bucketId)
            ?? throw new InvalidOperationException("Bucket not found.");
        var normalizedNote = AnesthesiaFieldValidator.NormalizeNote(noteText);

        bucket.Notes = string.IsNullOrWhiteSpace(bucket.Notes)
            ? normalizedNote
            : $"{bucket.Notes}\n{normalizedNote}";

        await _repository.SaveBucketAsync(bucket);
    }
}
