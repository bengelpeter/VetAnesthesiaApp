namespace VetAnesthesiaApp.Services.Voice;

public interface IVoiceParserService
{
    VoiceCommandResult Parse(string transcript, DateTime now);
}