using System;
using System.Collections.Generic;
using System.Text;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Data
{
    public interface IAnesthesiaRepository
    {
        Task InitializeAsync();
        Task<List<Animal>> GetAnimalsAsync();
        Task<Animal?> GetAnimalAsync(Guid animalId);
        Task SaveAnimalAsync(Animal animal);
        Task DeleteAnimalAsync(Guid animalId);
        Task<List<AnesthesiaSession>> GetSessionsAsync();
        Task<AnesthesiaSession?> GetSessionAsync(Guid sessionId);
        Task SaveSessionAsync(AnesthesiaSession session);
        Task<ClinicSettings> GetClinicSettingsAsync();
        Task SaveClinicSettingsAsync(ClinicSettings settings);

        Task<List<AnesthesiaBucket>> GetBucketsAsync(Guid sessionId);
        Task<AnesthesiaBucket?> GetBucketAsync(Guid bucketId);
        Task SaveBucketAsync(AnesthesiaBucket bucket);
        Task DeleteBucketAsync(Guid bucketId);

        Task<List<VoiceEntryLog>> GetVoiceLogsAsync(Guid sessionId);
        Task SaveVoiceLogAsync(VoiceEntryLog log);
        Task<List<AnesthesiaSession>> GetSessionsByAnimalAsync(Guid animalId);
    }
}
