using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;

namespace VetAnesthesiaApp.Tests;

internal sealed class InMemoryAnesthesiaRepository : IAnesthesiaRepository
{
    private readonly Dictionary<Guid, Animal> _animals = new();
    private readonly Dictionary<Guid, AnesthesiaSession> _sessions = new();
    private readonly Dictionary<Guid, AnesthesiaBucket> _buckets = new();
    private readonly Dictionary<Guid, VoiceEntryLog> _voiceLogs = new();
    private ClinicSettings _clinicSettings = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task<List<Animal>> GetAnimalsAsync() =>
        Task.FromResult(_animals.Values.OrderBy(x => x.Name).ToList());

    public Task<Animal?> GetAnimalAsync(Guid animalId) =>
        Task.FromResult(_animals.TryGetValue(animalId, out var animal) ? animal : null);

    public Task SaveAnimalAsync(Animal animal)
    {
        _animals[animal.Id] = animal;
        return Task.CompletedTask;
    }

    public Task DeleteAnimalAsync(Guid animalId)
    {
        _animals.Remove(animalId);
        return Task.CompletedTask;
    }

    public Task<List<AnesthesiaSession>> GetSessionsAsync() =>
        Task.FromResult(_sessions.Values.ToList());

    public Task<AnesthesiaSession?> GetSessionAsync(Guid sessionId) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);

    public Task SaveSessionAsync(AnesthesiaSession session)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<ClinicSettings> GetClinicSettingsAsync() =>
        Task.FromResult(_clinicSettings);

    public Task SaveClinicSettingsAsync(ClinicSettings settings)
    {
        _clinicSettings = settings;
        return Task.CompletedTask;
    }

    public Task<List<AnesthesiaBucket>> GetBucketsAsync(Guid sessionId) =>
        Task.FromResult(_buckets.Values
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.BucketStartTime)
            .ToList());

    public Task<AnesthesiaBucket?> GetBucketAsync(Guid bucketId) =>
        Task.FromResult(_buckets.TryGetValue(bucketId, out var bucket) ? bucket : null);

    public Task SaveBucketAsync(AnesthesiaBucket bucket)
    {
        _buckets[bucket.Id] = bucket;
        return Task.CompletedTask;
    }

    public Task DeleteBucketAsync(Guid bucketId)
    {
        _buckets.Remove(bucketId);
        return Task.CompletedTask;
    }

    public Task<List<VoiceEntryLog>> GetVoiceLogsAsync(Guid sessionId) =>
        Task.FromResult(_voiceLogs.Values
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.SpokenAt)
            .ToList());

    public Task SaveVoiceLogAsync(VoiceEntryLog log)
    {
        _voiceLogs[log.Id] = log;
        return Task.CompletedTask;
    }

    public Task<List<AnesthesiaSession>> GetSessionsByAnimalAsync(Guid animalId) =>
        Task.FromResult(_sessions.Values
            .Where(x => x.AnimalId == animalId)
            .OrderByDescending(x => x.SessionStartTime)
            .ToList());
}
