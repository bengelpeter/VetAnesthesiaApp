namespace VetAnesthesiaApp.Services.Speech;

public sealed record SpeechStatusUpdate(SpeechStatusLevel Level, string Message);

public enum SpeechStatusLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}
