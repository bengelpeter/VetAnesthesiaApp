using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public sealed record ChartFieldDefinition(
    string Key,
    string Label,
    bool IsWholeNumber,
    Func<AnesthesiaBucket, decimal?> ValueSelector);
