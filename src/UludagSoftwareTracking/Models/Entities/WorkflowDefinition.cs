using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class WorkflowDefinition
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
}
