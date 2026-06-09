using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Buckets;

namespace VetAnesthesiaApp.Tests;

public class BucketServiceTests
{
    [Fact]
    public async Task GetOrCreateActiveBucketAsync_CreatesBucketAndMarksSessionActive()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var session = new AnesthesiaSession { AnimalId = Guid.NewGuid() };
        await repo.SaveSessionAsync(session);
        var now = new DateTime(2026, 6, 8, 9, 0, 0);

        var bucket = await service.GetOrCreateActiveBucketAsync(session.Id, now);
        var reloadedSession = await repo.GetSessionAsync(session.Id);

        Assert.Equal(now, bucket.BucketStartTime);
        Assert.Equal(bucket.Id, reloadedSession!.ActiveBucketId);
    }

    [Fact]
    public async Task StartNewBucketAsync_ClosesExistingBucketBeforeOpeningNext()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var session = new AnesthesiaSession { AnimalId = Guid.NewGuid() };
        await repo.SaveSessionAsync(session);
        var firstTime = new DateTime(2026, 6, 8, 9, 0, 0);
        var secondTime = firstTime.AddMinutes(5);

        var firstBucket = await service.StartNewBucketAsync(session.Id, firstTime);
        var secondBucket = await service.StartNewBucketAsync(session.Id, secondTime);
        var reloadedFirst = await repo.GetBucketAsync(firstBucket.Id);
        var reloadedSession = await repo.GetSessionAsync(session.Id);

        Assert.Equal(secondTime, reloadedFirst!.BucketEndTime);
        Assert.Equal(secondBucket.Id, reloadedSession!.ActiveBucketId);
    }

    [Fact]
    public async Task ApplyFieldValueAsync_PersistsTypedValues()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var bucket = new AnesthesiaBucket { SessionId = Guid.NewGuid(), BucketStartTime = DateTime.Now };
        await repo.SaveBucketAsync(bucket);

        await service.ApplyFieldValueAsync(bucket.Id, AnesthesiaFieldKeys.IsoPercent, 2.5m);
        await service.ApplyFieldValueAsync(bucket.Id, AnesthesiaFieldKeys.Spo2, 97m);
        var reloaded = await repo.GetBucketAsync(bucket.Id);

        Assert.Equal(2.5m, reloaded!.IsoPercent);
        Assert.Equal(97, reloaded.Spo2);
    }

    [Fact]
    public async Task ApplyFieldValueAsync_RejectsOutOfRangeValues()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var bucket = new AnesthesiaBucket { SessionId = Guid.NewGuid(), BucketStartTime = DateTime.Now };
        await repo.SaveBucketAsync(bucket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyFieldValueAsync(bucket.Id, AnesthesiaFieldKeys.Spo2, 105m));

        var reloaded = await repo.GetBucketAsync(bucket.Id);

        Assert.Equal("SpO2 must be between 0 and 100.", ex.Message);
        Assert.Null(reloaded!.Spo2);
    }

    [Fact]
    public async Task ApplyFieldValueAsync_RejectsFractionsForWholeNumberFields()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var bucket = new AnesthesiaBucket { SessionId = Guid.NewGuid(), BucketStartTime = DateTime.Now };
        await repo.SaveBucketAsync(bucket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyFieldValueAsync(bucket.Id, AnesthesiaFieldKeys.HeartRate, 98.5m));

        var reloaded = await repo.GetBucketAsync(bucket.Id);

        Assert.Equal("Heart rate must be a whole number.", ex.Message);
        Assert.Null(reloaded!.HeartRate);
    }

    [Fact]
    public async Task StartNewBucketAsync_RejectsEarlierTimestampThanActiveBucket()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var session = new AnesthesiaSession { AnimalId = Guid.NewGuid() };
        await repo.SaveSessionAsync(session);
        var firstTime = new DateTime(2026, 6, 8, 9, 0, 0);

        await service.StartNewBucketAsync(session.Id, firstTime);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartNewBucketAsync(session.Id, firstTime.AddMinutes(-5)));

        var buckets = await repo.GetBucketsAsync(session.Id);

        Assert.Equal("New bucket time cannot be earlier than the active bucket start time.", ex.Message);
        Assert.Single(buckets);
    }

    [Fact]
    public async Task AppendNoteAsync_AppendsOnNewLines()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var bucket = new AnesthesiaBucket { SessionId = Guid.NewGuid(), BucketStartTime = DateTime.Now };
        await repo.SaveBucketAsync(bucket);

        await service.AppendNoteAsync(bucket.Id, "first note");
        await service.AppendNoteAsync(bucket.Id, "second note");
        var reloaded = await repo.GetBucketAsync(bucket.Id);

        Assert.Equal("first note\nsecond note", reloaded!.Notes);
    }

    [Fact]
    public async Task AppendNoteAsync_RejectsBlankNotes()
    {
        var repo = new InMemoryAnesthesiaRepository();
        var service = new BucketService(repo);
        var bucket = new AnesthesiaBucket { SessionId = Guid.NewGuid(), BucketStartTime = DateTime.Now };
        await repo.SaveBucketAsync(bucket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AppendNoteAsync(bucket.Id, "   "));

        var reloaded = await repo.GetBucketAsync(bucket.Id);

        Assert.Equal("Note text cannot be empty.", ex.Message);
        Assert.Null(reloaded!.Notes);
    }
}
