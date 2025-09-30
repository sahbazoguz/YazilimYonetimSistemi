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

    [StringLength(150)]
    public string? Role { get; set; }

    [StringLength(50)]
    public string? NextStepCode { get; set; }

    public int DisplayOrder { get; set; }
}
