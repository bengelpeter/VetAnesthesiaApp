namespace VetAnesthesiaApp.Services.Speech;

public interface IVetSpeechService
{
    bool IsListening { get; }

    event Action<string>? SpeechRecognized;
    event Action<bool>? ListeningChanged;
    event Action<SpeechStatusUpdate>? StatusChanged;

    Task StartAsync();
    Task StopAsync();
}
