using VetAnesthesiaApp.Models;

namespace VetAnesthesiaApp.Services.Workflow;

public interface IWorkflowTemplateService
{
    Task<List<WorkflowTemplate>> GetTemplatesAsync();
    Task<WorkflowTemplate> SaveTemplateFromSettingsAsync(string name, string? description, ClinicSettings settings);
    void ApplyTemplateToSettings(WorkflowTemplate template, ClinicSettings settings);
    Task DeleteTemplateAsync(Guid templateId);
}
