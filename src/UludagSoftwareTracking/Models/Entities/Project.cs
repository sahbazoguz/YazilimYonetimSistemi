using System.ComponentModel.DataAnnotations;

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

    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
}
