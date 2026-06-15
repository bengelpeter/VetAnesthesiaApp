using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Buckets;
using VetAnesthesiaApp.Services.Data;

namespace VetAnesthesiaApp.Services.Voice;

public class VoiceCommandApplicationService : IVoiceCommandApplicationService
{
    private readonly IVoiceParserService _parser;
    private readonly IBucketService _bucketService;
    private readonly IAnesthesiaRepository _repository;

    public VoiceCommandApplicationService(
        IVoiceParserService parser,
        IBucketService bucketService,
        IAnesthesiaRepository repository)
    {
        _parser = parser;
        _bucketService = bucketService;
        _repository = repository;
    }

    public async Task<VoiceEntryLog> ProcessTranscriptAsync(Guid sessionId, string transcript, DateTime spokenAt)
    {
        var result = _parser.Parse(transcript, spokenAt);

        if (result.CommandType == VoiceCommandType.Undo)
            return await UndoLastAppliedCommandAsync(sessionId, spokenAt, transcript);

        var log = CreateLog(sessionId, spokenAt, result);

        if (!result.IsSuccess)
        {
            await _repository.SaveVoiceLogAsync(log);
            return log;
        }

        try
        {
            switch (result.CommandType)
            {
                case VoiceCommandType.FieldValue:
                    {
                        var bucket = await _bucketService.GetOrCreateActiveBucketAsync(sessionId, spokenAt);
                        log.PreviousNumericValue = GetFieldValue(bucket, result.FieldKey!);
                        await _bucketService.ApplyFieldValueAsync(bucket.Id, result.FieldKey!, result.ParsedNumericValue!.Value);
                        log.BucketId = bucket.Id;
                        log.Applied = true;
                        log.StatusMessage = $"{result.FieldKey} applied to active bucket.";
                        break;
                    }
                case VoiceCommandType.Note:
                    {
                        var bucket = await _bucketService.GetOrCreateActiveBucketAsync(sessionId, spokenAt);
                        log.PreviousTextValue = bucket.Notes;
                        await _bucketService.AppendNoteAsync(bucket.Id, result.NoteText!);
                        log.BucketId = bucket.Id;
                        log.Applied = true;
                        log.StatusMessage = "Note applied to active bucket.";
                        break;
                    }
                case VoiceCommandType.NextBucket:
                    {
                        var previousBucket = await _bucketService.GetActiveBucketAsync(sessionId);
                        var previousBucketEndTime = previousBucket?.BucketEndTime;
                        var bucket = await _bucketService.StartNewBucketAsync(sessionId, spokenAt);
                        log.BucketId = bucket.Id;
                        log.PreviousBucketId = previousBucket?.Id;
                        log.PreviousBucketEndTime = previousBucketEndTime;
                        log.Applied = true;
                        log.StatusMessage = "New bucket started.";
                        break;
                    }
                default:
                    {
                        log.StatusMessage = $"{result.CommandType} command is not supported.";
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            log.Applied = false;
            log.StatusMessage = $"Apply failed: {ex.Message}";
            await _repository.SaveVoiceLogAsync(log);
            throw;
        }

        await _repository.SaveVoiceLogAsync(log);
        return log;
    }

    public async Task<VoiceEntryLog> UndoLastAppliedCommandAsync(Guid sessionId, DateTime undoneAt, string transcript)
    {
        var undoLog = new VoiceEntryLog
        {
            SessionId = sessionId,
            SpokenAt = undoneAt,
            RawTranscript = transcript,
            NormalizedTranscript = transcript.Trim().ToLowerInvariant(),
            MatchedCommandType = VoiceCommandType.Undo.ToString(),
            Applied = false,
            StatusMessage = "Nothing to undo."
        };

        var targetLog = (await _repository.GetVoiceLogsAsync(sessionId))
            .FirstOrDefault(x => x.Applied && !x.Undone && !string.Equals(x.MatchedCommandType, VoiceCommandType.Undo.ToString(), StringComparison.OrdinalIgnoreCase));

        if (targetLog is null)
        {
            await _repository.SaveVoiceLogAsync(undoLog);
            return undoLog;
        }

        try
        {
            switch (targetLog.MatchedCommandType)
            {
                case nameof(VoiceCommandType.FieldValue):
                    await UndoFieldValueAsync(targetLog);
                    undoLog.BucketId = targetLog.BucketId;
                    undoLog.MatchedFieldKey = targetLog.MatchedFieldKey;
                    undoLog.StatusMessage = $"Undid last {targetLog.MatchedFieldKey} entry.";
                    break;
                case nameof(VoiceCommandType.Note):
                    await UndoNoteAsync(targetLog);
                    undoLog.BucketId = targetLog.BucketId;
                    undoLog.StatusMessage = "Undid last note entry.";
                    break;
                case nameof(VoiceCommandType.NextBucket):
                    await UndoNextBucketAsync(sessionId, targetLog);
                    undoLog.BucketId = targetLog.PreviousBucketId;
                    undoLog.StatusMessage = "Undid last bucket change.";
                    break;
                default:
                    undoLog.StatusMessage = $"Undo is not supported for {targetLog.MatchedCommandType}.";
                    await _repository.SaveVoiceLogAsync(undoLog);
                    return undoLog;
            }

            targetLog.Undone = true;
            targetLog.UndoneAt = undoneAt;
            await _repository.SaveVoiceLogAsync(targetLog);

            undoLog.Applied = true;
            await _repository.SaveVoiceLogAsync(undoLog);
            return undoLog;
        }
        catch (Exception ex)
        {
            undoLog.StatusMessage = $"Undo failed: {ex.Message}";
            await _repository.SaveVoiceLogAsync(undoLog);
            throw;
        }
    }

    private static VoiceEntryLog CreateLog(Guid sessionId, DateTime spokenAt, VoiceCommandResult result)
    {
        return new VoiceEntryLog
        {
            SessionId = sessionId,
            SpokenAt = spokenAt,
            RawTranscript = result.RawTranscript,
            NormalizedTranscript = result.NormalizedTranscript,
            MatchedCommandType = result.CommandType.ToString(),
            MatchedFieldKey = result.FieldKey,
            ParsedValueText = result.ParsedValueText,
            ParsedNumericValue = result.ParsedNumericValue,
            Applied = false,
            StatusMessage = result.StatusMessage
        };
    }

    private async Task UndoFieldValueAsync(VoiceEntryLog targetLog)
    {
        if (targetLog.BucketId is null || string.IsNullOrWhiteSpace(targetLog.MatchedFieldKey))
            throw new InvalidOperationException("The original field entry does not contain enough information to undo.");

        if (targetLog.PreviousNumericValue.HasValue)
        {
            await _bucketService.ApplyFieldValueAsync(targetLog.BucketId.Value, targetLog.MatchedFieldKey, targetLog.PreviousNumericValue.Value);
            return;
        }

        await _bucketService.ClearFieldAsync(targetLog.BucketId.Value, targetLog.MatchedFieldKey);
    }

    private async Task UndoNoteAsync(VoiceEntryLog targetLog)
    {
        if (targetLog.BucketId is null)
            throw new InvalidOperationException("The original note entry does not contain enough information to undo.");

        var bucket = await _repository.GetBucketAsync(targetLog.BucketId.Value)
            ?? throw new InvalidOperationException("The original bucket for this note was not found.");

        bucket.Notes = targetLog.PreviousTextValue;
        await _repository.SaveBucketAsync(bucket);
    }

    private async Task UndoNextBucketAsync(Guid sessionId, VoiceEntryLog targetLog)
    {
        if (targetLog.BucketId is null)
            throw new InvalidOperationException("The original bucket change does not contain enough information to undo.");

        var createdBucket = await _repository.GetBucketAsync(targetLog.BucketId.Value)
            ?? throw new InvalidOperationException("The newest bucket was not found.");

        if (!IsBucketEmpty(createdBucket))
            throw new InvalidOperationException("The newest bucket already has data, so it cannot be undone safely.");

        var buckets = await _repository.GetBucketsAsync(sessionId);
        if (buckets.Count == 0 || buckets[^1].Id != createdBucket.Id)
            throw new InvalidOperationException("Only the newest bucket can be undone.");

        await _repository.DeleteBucketAsync(createdBucket.Id);

        if (targetLog.PreviousBucketId.HasValue)
        {
            var previousBucket = await _repository.GetBucketAsync(targetLog.PreviousBucketId.Value)
                ?? throw new InvalidOperationException("The previous bucket was not found.");

            previousBucket.BucketEndTime = targetLog.PreviousBucketEndTime;
            await _repository.SaveBucketAsync(previousBucket);
        }

        var session = await _repository.GetSessionAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found.");
        session.ActiveBucketId = targetLog.PreviousBucketId;
        await _repository.SaveSessionAsync(session);
    }

    private static decimal? GetFieldValue(AnesthesiaBucket bucket, string fieldKey) =>
        fieldKey switch
        {
            nameof(AnesthesiaBucket.IsoPercent) => bucket.IsoPercent,
            nameof(AnesthesiaBucket.OxygenFlowRate) => bucket.OxygenFlowRate,
            nameof(AnesthesiaBucket.Etco2) => bucket.Etco2,
            nameof(AnesthesiaBucket.Spo2) => bucket.Spo2,
            nameof(AnesthesiaBucket.Temperature) => bucket.Temperature,
            nameof(AnesthesiaBucket.HeartRate) => bucket.HeartRate,
            nameof(AnesthesiaBucket.RespiratoryRate) => bucket.RespiratoryRate,
            nameof(AnesthesiaBucket.SystolicBp) => bucket.SystolicBp,
            nameof(AnesthesiaBucket.DiastolicBp) => bucket.DiastolicBp,
            nameof(AnesthesiaBucket.Map) => bucket.Map,
            _ => throw new InvalidOperationException($"Unsupported field key: {fieldKey}")
        };

    private static bool IsBucketEmpty(AnesthesiaBucket bucket)
    {
        return bucket.IsoPercent is null
            && bucket.OxygenFlowRate is null
            && bucket.Etco2 is null
            && bucket.Spo2 is null
            && bucket.Temperature is null
            && bucket.HeartRate is null
            && bucket.RespiratoryRate is null
            && bucket.SystolicBp is null
            && bucket.DiastolicBp is null
            && bucket.Map is null
            && string.IsNullOrWhiteSpace(bucket.Notes);
    }
}
