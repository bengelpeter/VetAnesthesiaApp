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
        await _db.CreateTableAsync<ClinicSettings>();
        await _db.CreateTableAsync<VoiceEntryLog>();
        await EnsureVoiceLogColumnsAsync();
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

    public async Task<ClinicSettings> GetClinicSettingsAsync()
    {
        var settings = await Db.Table<ClinicSettings>()
            .Where(x => x.Id == 1)
            .FirstOrDefaultAsync();

        if (settings is not null)
            return settings;

        settings = new ClinicSettings();
        await Db.InsertAsync(settings);
        return settings;
    }

    public async Task SaveClinicSettingsAsync(ClinicSettings settings)
    {
        settings.Id = 1;
        var existing = await Db.Table<ClinicSettings>()
            .Where(x => x.Id == 1)
            .FirstOrDefaultAsync();

        if (existing is null)
            await Db.InsertAsync(settings);
        else
            await Db.UpdateAsync(settings);
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

    public Task DeleteBucketAsync(Guid bucketId) =>
        Db.DeleteAsync<AnesthesiaBucket>(bucketId);

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

    private async Task EnsureVoiceLogColumnsAsync()
    {
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.PreviousNumericValue), "REAL");
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.PreviousTextValue), "TEXT");
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.PreviousBucketId), "TEXT");
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.PreviousBucketEndTime), "TEXT");
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.Undone), "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(nameof(VoiceEntryLog), nameof(VoiceEntryLog.UndoneAt), "TEXT");
    }

    private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition)
    {
        var existingColumns = await Db.GetTableInfoAsync(tableName);
        if (existingColumns.Any(x => string.Equals(x.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            return;

        await Db.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
    }
}
