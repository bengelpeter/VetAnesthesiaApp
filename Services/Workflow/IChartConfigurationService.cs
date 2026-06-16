using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface IChartConfigurationService
{
    IReadOnlyList<ChartFieldDefinition> GetConfiguredFields(ClinicSettings settings);
    IReadOnlyCollection<string> GetRequiredCompletionFieldKeys(ClinicSettings settings);
    Dictionary<string, string> GetLabelMap(ClinicSettings settings);
}
