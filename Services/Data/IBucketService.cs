using System;
using System.Collections.Generic;
using System.Text;
using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Data
{
    public interface IBucketService
    {
        Task<AnesthesiaBucket> GetOrCreateActiveBucketAsync(Guid sessionId, DateTime now);
        Task<AnesthesiaBucket> StartNewBucketAsync(Guid sessionId, DateTime bucketStartTime);
        Task<AnesthesiaBucket?> GetActiveBucketAsync(Guid sessionId);

        Task ApplyFieldValueAsync(Guid bucketId, string fieldKey, decimal value);
        Task ClearFieldAsync(Guid bucketId, string fieldKey);
        Task AppendNoteAsync(Guid bucketId, string noteText);
    }
}
