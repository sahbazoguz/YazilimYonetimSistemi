using System.Collections.Generic;

namespace UludagSoftwareTracking.Models.ViewModels;

public class WorkflowStepViewModel
{
    public int Id { get; init; }

    public string SequenceCode { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? Role { get; init; }

    public string? NextStepCode { get; init; }
}

public class WorkflowDefinitionViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<WorkflowStepViewModel> Steps { get; init; } = new List<WorkflowStepViewModel>();
}

public class WorkflowEditorPostModel
{
    public List<WorkflowEditorDefinition> Workflows { get; set; } = new();
}

public class WorkflowEditorDefinition
{
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<WorkflowEditorStep> Steps { get; set; } = new();
}

public class WorkflowEditorStep
{
    public int? Id { get; set; }

    public string SequenceCode { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Role { get; set; }

    public string? NextStepCode { get; set; }
}
