using SQLite;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Data;

public class SqliteAnesthesiaRepository : IAnesthesiaRepository
{
    private SQLiteAsyncConnection? _db;

    public async Task InitializeAsync()
    {
        if (_db is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "anesthesia.db");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<Animal>();
        await _db.CreateTableAsync<AnesthesiaSession>();
        await _db.CreateTableAsync<AnesthesiaBucket>();
        await _db.CreateTableAsync<VoiceEntryLog>();
    }

    private SQLiteAsyncConnection Db =>
     _db ?? throw new InvalidOperationException("Database has not been initialized.");

    public async Task<List<Animal>> GetAnimalsAsync()
    => await Db.Table<Animal>()
        .OrderBy(x => x.Name)
        .ToListAsync();

    public async Task<Animal?> GetAnimalAsync(Guid animalId)
        => await Db.Table<Animal>()
            .Where(x => x.Id == animalId)
            .FirstOrDefaultAsync();

    public async Task SaveAnimalAsync(Animal animal)
    {
        var existing = await GetAnimalAsync(animal.Id);

        if (existing is null)
            await Db.InsertAsync(animal);
        else
            await Db.UpdateAsync(animal);
    }

    public async Task DeleteAnimalAsync(Guid animalId)
    {
        await Db.DeleteAsync<Animal>(animalId);
    }

    public async Task<List<AnesthesiaSession>> GetSessionsAsync()
        => await Db.Table<AnesthesiaSession>().ToListAsync();

    public async Task<AnesthesiaSession?> GetSessionAsync(Guid sessionId)
        => await Db.Table<AnesthesiaSession>()
            .Where(x => x.Id == sessionId)
            .FirstOrDefaultAsync();

    public async Task SaveSessionAsync(AnesthesiaSession session)
    {
        var existing = await GetSessionAsync(session.Id);

        if (existing is null)
            await Db.InsertAsync(session);
        else
            await Db.UpdateAsync(session);
    }

    public async Task<List<AnesthesiaBucket>> GetBucketsAsync(Guid sessionId)
        => await Db.Table<AnesthesiaBucket>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.BucketStartTime)
            .ToListAsync();

    public async Task<AnesthesiaBucket?> GetBucketAsync(Guid bucketId)
        => await Db.Table<AnesthesiaBucket>()
            .Where(x => x.Id == bucketId)
            .FirstOrDefaultAsync();

    public async Task SaveBucketAsync(AnesthesiaBucket bucket)
    {
        var existing = await GetBucketAsync(bucket.Id);

        if (existing is null)
            await Db.InsertAsync(bucket);
        else
            await Db.UpdateAsync(bucket);
    }

    public async Task<List<VoiceEntryLog>> GetVoiceLogsAsync(Guid sessionId)
        => await Db.Table<VoiceEntryLog>()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.SpokenAt)
            .ToListAsync();

    public async Task SaveVoiceLogAsync(VoiceEntryLog log)
    {
        var existing = await Db.Table<VoiceEntryLog>()
            .Where(x => x.Id == log.Id)
            .FirstOrDefaultAsync();

        if (existing is null)
            await Db.InsertAsync(log);
        else
            await Db.UpdateAsync(log);
    }

    public async Task<List<AnesthesiaSession>> GetSessionsByAnimalAsync(Guid animalId)
    => await Db.Table<AnesthesiaSession>()
        .Where(x => x.AnimalId == animalId)
        .OrderByDescending(x => x.SessionStartTime)
        .ToListAsync();
}