using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public sealed record SessionCompletionItem(
    string Key,
    string Label,
    bool IsComplete,
    bool BlocksHandoff,
    VetAlertLevel Level,
    string Message);
