using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UludagSoftwareTracking.Models.Entities;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planlama;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int RequestId { get; set; }

    public SoftwareRequest? Request { get; set; }

    public int? LeadUserId { get; set; }

    public UserProfile? LeadUser { get; set; }

    public int? ReleasedSoftwareId { get; set; }

    public Software? ReleasedSoftware { get; set; }

    [StringLength(200)]
    public string? TechnicalGuidePath { get; set; }

    [StringLength(200)]
    public string? UserGuidePath { get; set; }

    public DateTime? CompletedOn { get; set; }

    public DateTime? TestConfirmedOn { get; set; }

    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();

    [InverseProperty(nameof(WorkflowDefinition.Project))]
    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; } = new List<WorkflowDefinition>();
}
