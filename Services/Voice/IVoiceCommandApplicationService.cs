using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Voice;

public interface IVoiceCommandApplicationService
{
    Task<VoiceEntryLog> ProcessTranscriptAsync(Guid sessionId, string transcript, DateTime spokenAt);
    Task<VoiceEntryLog> UndoLastAppliedCommandAsync(Guid sessionId, DateTime undoneAt, string transcript);
}
