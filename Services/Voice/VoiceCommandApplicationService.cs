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

        var log = new VoiceEntryLog
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

        if (!result.IsSuccess)
        {
            await _repository.SaveVoiceLogAsync(log);
            return log;
        }

        switch (result.CommandType)
        {
            case VoiceCommandType.FieldValue:
                {
                    var bucket = await _bucketService.GetOrCreateActiveBucketAsync(sessionId, spokenAt);
                    await _bucketService.ApplyFieldValueAsync(bucket.Id, result.FieldKey!, result.ParsedNumericValue!.Value);
                    log.BucketId = bucket.Id;
                    log.Applied = true;
                    log.StatusMessage = $"{result.FieldKey} applied to active bucket.";
                    break;
                }
            case VoiceCommandType.Note:
                {
                    var bucket = await _bucketService.GetOrCreateActiveBucketAsync(sessionId, spokenAt);
                    await _bucketService.AppendNoteAsync(bucket.Id, result.NoteText!);
                    log.BucketId = bucket.Id;
                    log.Applied = true;
                    log.StatusMessage = "Note applied to active bucket.";
                    break;
                }
            case VoiceCommandType.NextBucket:
                {
                    var bucket = await _bucketService.StartNewBucketAsync(sessionId, spokenAt);
                    log.BucketId = bucket.Id;
                    log.Applied = true;
                    log.StatusMessage = "New bucket started.";
                    break;
                }
        }

        await _repository.SaveVoiceLogAsync(log);
        return log;
    }
}