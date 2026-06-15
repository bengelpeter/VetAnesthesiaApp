using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Buckets;
using VetAnesthesiaApp.Services.Voice;

namespace VetAnesthesiaApp.Tests;

public class VoiceCommandApplicationServiceTests
{
    [Fact]
    public async Task ProcessTranscriptAsync_AppliesFieldValueAndLogsIt()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);

        var log = await service.ProcessTranscriptAsync(session.Id, "heart rate ninety eight", new DateTime(2026, 6, 8, 9, 0, 0));
        var buckets = await repo.GetBucketsAsync(session.Id);

        Assert.True(log.Applied);
        Assert.Single(buckets);
        Assert.Equal(98, buckets[0].HeartRate);
        Assert.Equal(buckets[0].Id, log.BucketId);
        Assert.Equal($"{AnesthesiaFieldKeys.HeartRate} applied to active bucket.", log.StatusMessage);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_StoresFailedParsesForAudit()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);

        var log = await service.ProcessTranscriptAsync(session.Id, "just checking in", DateTime.Now);
        var savedLogs = await repo.GetVoiceLogsAsync(session.Id);

        Assert.False(log.Applied);
        Assert.Equal("No command match.", log.StatusMessage);
        Assert.Single(savedLogs);
        Assert.Equal(log.Id, savedLogs[0].Id);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_StartsNewBucketWhenRequested()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);
        var firstTime = new DateTime(2026, 6, 8, 9, 0, 0);
        var secondTime = firstTime.AddMinutes(5);

        await service.ProcessTranscriptAsync(session.Id, "heart rate 90", firstTime);
        var nextLog = await service.ProcessTranscriptAsync(session.Id, "next bucket", secondTime);
        var buckets = await repo.GetBucketsAsync(session.Id);

        Assert.True(nextLog.Applied);
        Assert.Equal(2, buckets.Count);
        Assert.Equal(secondTime, buckets[0].BucketEndTime);
        Assert.Equal(secondTime, buckets[1].BucketStartTime);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_RecordsNotesOnActiveBucket()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);

        await service.ProcessTranscriptAsync(session.Id, "note jaw tone light", DateTime.Now);
        var bucket = (await repo.GetBucketsAsync(session.Id)).Single();

        Assert.Equal("jaw tone light", bucket.Notes);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_UndoClearsLastAppliedFieldValue()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);
        var spokenAt = new DateTime(2026, 6, 8, 9, 0, 0);

        await service.ProcessTranscriptAsync(session.Id, "heart rate 98", spokenAt);
        var log = await service.ProcessTranscriptAsync(session.Id, "undo", spokenAt.AddSeconds(10));
        var bucket = (await repo.GetBucketsAsync(session.Id)).Single();
        var logs = await repo.GetVoiceLogsAsync(session.Id);

        Assert.True(log.Applied);
        Assert.Equal("Undid last HeartRate entry.", log.StatusMessage);
        Assert.Null(bucket.HeartRate);
        Assert.Contains(logs, x => x.Undone && x.MatchedFieldKey == AnesthesiaFieldKeys.HeartRate);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_UndoRestoresPreviousNoteText()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);
        var spokenAt = new DateTime(2026, 6, 8, 9, 0, 0);

        await service.ProcessTranscriptAsync(session.Id, "note stable plane", spokenAt);
        await service.ProcessTranscriptAsync(session.Id, "note warming started", spokenAt.AddMinutes(1));
        var log = await service.ProcessTranscriptAsync(session.Id, "undo", spokenAt.AddMinutes(2));
        var bucket = (await repo.GetBucketsAsync(session.Id)).Single();

        Assert.True(log.Applied);
        Assert.Equal("Undid last note entry.", log.StatusMessage);
        Assert.Equal("stable plane", bucket.Notes);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_UndoRemovesNewestEmptyBucket()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);
        var firstTime = new DateTime(2026, 6, 8, 9, 0, 0);
        var secondTime = firstTime.AddMinutes(5);

        await service.ProcessTranscriptAsync(session.Id, "heart rate 90", firstTime);
        await service.ProcessTranscriptAsync(session.Id, "next bucket", secondTime);

        var log = await service.ProcessTranscriptAsync(session.Id, "undo", secondTime.AddSeconds(10));
        var buckets = await repo.GetBucketsAsync(session.Id);
        var savedSession = await repo.GetSessionAsync(session.Id);

        Assert.True(log.Applied);
        Assert.Equal("Undid last bucket change.", log.StatusMessage);
        Assert.Single(buckets);
        Assert.Null(buckets[0].BucketEndTime);
        Assert.Equal(buckets[0].Id, savedSession!.ActiveBucketId);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_PersistsFailureLogBeforeRethrowingApplyErrors()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var missingSessionId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessTranscriptAsync(missingSessionId, "heart rate 90", DateTime.Now));

        var logs = await repo.GetVoiceLogsAsync(missingSessionId);

        Assert.Equal("Session not found.", ex.Message);
        Assert.Single(logs);
        Assert.False(logs[0].Applied);
        Assert.StartsWith("Apply failed: Session not found.", logs[0].StatusMessage);
    }

    [Fact]
    public async Task ProcessTranscriptAsync_LogsValidationFailuresBeforeRethrowing()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = CreateService(repo);
        var session = await CreateSessionAsync(repo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessTranscriptAsync(session.Id, "oxygen saturation 110", DateTime.Now));

        var buckets = await repo.GetBucketsAsync(session.Id);
        var logs = await repo.GetVoiceLogsAsync(session.Id);

        Assert.Equal("SpO2 must be between 0 and 100.", ex.Message);
        Assert.Single(buckets);
        Assert.Null(buckets[0].Spo2);
        Assert.Single(logs);
        Assert.False(logs[0].Applied);
        Assert.StartsWith("Apply failed: SpO2 must be between 0 and 100.", logs[0].StatusMessage);
    }

    private static VoiceCommandApplicationService CreateService(InMemoryAnesthesiaRepository repo)
    {
        var parser = new VoiceParserService(new SpokenNumberParser());
        var bucketService = new BucketService(repo);
        return new VoiceCommandApplicationService(parser, bucketService, repo);
    }

    private static async Task<AnesthesiaSession> CreateSessionAsync(InMemoryAnesthesiaRepository repo)
    {
        var session = new AnesthesiaSession
        {
            AnimalId = Guid.NewGuid(),
            SessionStartTime = new DateTime(2026, 6, 8, 8, 55, 0)
        };

        await repo.SaveSessionAsync(session);
        return session;
    }
}
