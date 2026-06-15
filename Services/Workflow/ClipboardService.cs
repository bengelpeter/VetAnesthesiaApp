using Microsoft.Maui.ApplicationModel;

namespace VetAnesthesiaApp.Services.Workflow;

public class ClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) =>
        Clipboard.Default.SetTextAsync(text);
}
