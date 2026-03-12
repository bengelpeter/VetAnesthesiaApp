using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace VetAnesthesiaApp.Services.Speech;

public class VetSpeechService : IVetSpeechService
{
    private readonly ISpeechToText _speech;
    private CancellationTokenSource? _cts;

    public bool IsListening { get; private set; }

    public event Action<string>? SpeechRecognized;
    public event Action<bool>? ListeningChanged;

    public VetSpeechService(ISpeechToText speech)
    {
        _speech = speech;
        _speech.RecognitionResultCompleted += OnRecognitionCompleted;
    }

    public async Task StartAsync()
    {
        if (IsListening)
            return;

        try
        {
            var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            var speechGranted = await _speech.RequestPermissions(CancellationToken.None);

            if (micStatus != PermissionStatus.Granted || !speechGranted)
                throw new PermissionException("Microphone permission not granted.");

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            IsListening = true;
            ListeningChanged?.Invoke(true);

            var options = new SpeechToTextOptions
            {
                Culture = CultureInfo.CurrentCulture,
                ShouldReportPartialResults = true
            };

            await _speech.StartListenAsync(options, _cts.Token);
        }
        catch
        {
            IsListening = false;
            ListeningChanged?.Invoke(false);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsListening)
            return;

        _cts?.Cancel();

        try
        {
            await _speech.StopListenAsync(CancellationToken.None);
        }
        catch
        {
        }

        IsListening = false;
        ListeningChanged?.Invoke(false);
    }

    private void OnRecognitionCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        var text = args.RecognitionResult?.Text;

        if (!string.IsNullOrWhiteSpace(text))
        {
            SpeechRecognized?.Invoke(text);
        }

        IsListening = false;
        ListeningChanged?.Invoke(false);
    }
}