using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class WorkflowStep
{
    public int Id { get; set; }

    public int WorkflowDefinitionId { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }

    [Required]
    [StringLength(20)]
    public string SequenceCode { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public WorkflowStepType StepType { get; set; } = WorkflowStepType.Normal;

    [StringLength(150)]
    public string? Role { get; set; }

    [StringLength(50)]
    public string? NextStepCode { get; set; }

    [StringLength(50)]
    public string? NextStepYesCode { get; set; }

    [StringLength(50)]
    public string? NextStepNoCode { get; set; }

    public int DisplayOrder { get; set; }
}
