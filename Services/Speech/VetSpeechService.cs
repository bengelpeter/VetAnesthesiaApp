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
    public event Action<SpeechStatusUpdate>? StatusChanged;

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
            StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Info, "Requesting microphone permission..."));
            var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            var speechGranted = await _speech.RequestPermissions(CancellationToken.None);

            if (micStatus != PermissionStatus.Granted || !speechGranted)
            {
                ResetListeningState("Microphone permission not granted. Enable microphone access for VetPulse in Android settings.");
                return;
            }

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            IsListening = true;
            ListeningChanged?.Invoke(true);
            StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Info, "Listening now. Speak your next command."));

            var options = new SpeechToTextOptions
            {
                Culture = CultureInfo.CurrentCulture,
                ShouldReportPartialResults = true
            };

            await _speech.StartListenAsync(options, _cts.Token);
        }
        catch (OperationCanceledException) when (_cts?.IsCancellationRequested == true)
        {
            ResetListeningState("Listening timed out before a command was captured.");
        }
        catch (Exception ex)
        {
            ResetListeningState($"Speech start failed: {ex.Message}");
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
        StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Info, "Stopped listening."));
    }

    private void OnRecognitionCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        var text = args.RecognitionResult?.Text;

        if (!string.IsNullOrWhiteSpace(text))
        {
            SpeechRecognized?.Invoke(text);
            StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Info, "Speech captured. Applying command..."));
        }
        else
        {
            StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Warning, "No speech detected. Try again closer to the tablet."));
        }

        ResetListeningState(null);
    }

    private void ResetListeningState(string? statusMessage)
    {
        IsListening = false;
        ListeningChanged?.Invoke(false);

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            StatusChanged?.Invoke(new SpeechStatusUpdate(SpeechStatusLevel.Error, statusMessage));
        }
    }
}
