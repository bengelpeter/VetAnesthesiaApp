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

        switch (fieldKey)
        {
            case AnesthesiaFieldKeys.IsoPercent:
                bucket.IsoPercent = value; break;
            case AnesthesiaFieldKeys.OxygenFlowRate:
                bucket.OxygenFlowRate = value; break;
            case AnesthesiaFieldKeys.Etco2:
                bucket.Etco2 = value; break;
            case AnesthesiaFieldKeys.Spo2:
                bucket.Spo2 = (int)value; break;
            case AnesthesiaFieldKeys.Temperature:
                bucket.Temperature = value; break;
            case AnesthesiaFieldKeys.HeartRate:
                bucket.HeartRate = (int)value; break;
            case AnesthesiaFieldKeys.RespiratoryRate:
                bucket.RespiratoryRate = (int)value; break;
            case AnesthesiaFieldKeys.SystolicBp:
                bucket.SystolicBp = (int)value; break;
            case AnesthesiaFieldKeys.DiastolicBp:
                bucket.DiastolicBp = (int)value; break;
            case AnesthesiaFieldKeys.Map:
                bucket.Map = (int)value; break;
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

        bucket.Notes = string.IsNullOrWhiteSpace(bucket.Notes)
            ? noteText
            : $"{bucket.Notes}\n{noteText}";

        await _repository.SaveBucketAsync(bucket);
    }
}