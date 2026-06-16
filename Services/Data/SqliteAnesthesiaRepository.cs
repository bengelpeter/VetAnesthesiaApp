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
        await _db.CreateTableAsync<WorkflowTemplate>();
        await _db.CreateTableAsync<SessionTelemetryEvent>();
        await EnsureClinicSettingsColumnsAsync();
        await EnsureVoiceLogColumnsAsync();
        await EnsureWorkflowTemplateColumnsAsync();
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

    public async Task<List<WorkflowTemplate>> GetWorkflowTemplatesAsync() =>
        await Db.Table<WorkflowTemplate>()
            .OrderBy(x => x.Name)
            .ToListAsync();

    public async Task<WorkflowTemplate?> GetWorkflowTemplateAsync(Guid templateId) =>
        await Db.Table<WorkflowTemplate>()
            .Where(x => x.Id == templateId)
            .FirstOrDefaultAsync();

    public async Task SaveWorkflowTemplateAsync(WorkflowTemplate template)
    {
        var existing = await GetWorkflowTemplateAsync(template.Id);

        if (existing is null)
            await Db.InsertAsync(template);
        else
            await Db.UpdateAsync(template);
    }

    public Task DeleteWorkflowTemplateAsync(Guid templateId) =>
        Db.DeleteAsync<WorkflowTemplate>(templateId);

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

    public async Task<List<SessionTelemetryEvent>> GetSessionTelemetryEventsAsync(Guid sessionId) =>
        await Db.Table<SessionTelemetryEvent>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();

    public async Task SaveSessionTelemetryEventAsync(SessionTelemetryEvent telemetryEvent)
    {
        var existing = await Db.Table<SessionTelemetryEvent>()
            .Where(x => x.Id == telemetryEvent.Id)
            .FirstOrDefaultAsync();

        if (existing is null)
            await Db.InsertAsync(telemetryEvent);
        else
            await Db.UpdateAsync(telemetryEvent);
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

    private async Task EnsureClinicSettingsColumnsAsync()
    {
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ClinicName), "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PreferredExportTargetKey), "TEXT NOT NULL DEFAULT 'ClinicChartNote'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PdfDocumentTitle), "TEXT NOT NULL DEFAULT 'Anesthesia Record'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ClinicChartExportLabel), "TEXT NOT NULL DEFAULT 'Clinic chart note'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ClinicChartExportNoteTitle), "TEXT NOT NULL DEFAULT 'Anesthesia chart note'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ClinicChartExportDescription), "TEXT NOT NULL DEFAULT 'Paste a complete anesthesia note directly into the patient chart.'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PdfAttachmentExportLabel), "TEXT NOT NULL DEFAULT 'PDF attachment note'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PdfAttachmentExportNoteTitle), "TEXT NOT NULL DEFAULT 'Anesthesia PDF attachment note'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PdfAttachmentExportDescription), "TEXT NOT NULL DEFAULT 'Attach the PDF record and paste a short note that points staff to it.'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.PdfAttachmentInstruction), "TEXT NOT NULL DEFAULT 'Attach the exported anesthesia PDF record to the patient chart.'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.CsvExportLabel), "TEXT NOT NULL DEFAULT 'Bucket CSV'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.CsvShareTitle), "TEXT NOT NULL DEFAULT 'Share bucket CSV'");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ChartFieldLabelsJson), "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.ChartFieldOrderCsv), "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(nameof(ClinicSettings), nameof(ClinicSettings.RequiredCompletionFieldKeysCsv), "TEXT NOT NULL DEFAULT 'HeartRate,RespiratoryRate,Spo2,Etco2,Temperature,Map'");
    }

    private async Task EnsureWorkflowTemplateColumnsAsync()
    {
        await EnsureColumnAsync(nameof(WorkflowTemplate), nameof(WorkflowTemplate.PreferredExportTargetKey), "TEXT NOT NULL DEFAULT 'ClinicChartNote'");
    }

    private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition)
    {
        var existingColumns = await Db.GetTableInfoAsync(tableName);
        if (existingColumns.Any(x => string.Equals(x.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            return;

        await Db.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
    }
}
