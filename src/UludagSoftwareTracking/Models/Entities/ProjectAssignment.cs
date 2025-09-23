using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class ProjectAssignment
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public int UserId { get; set; }

    public UserProfile? User { get; set; }

    [StringLength(200)]
    public string AssignedRole { get; set; } = string.Empty;

    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;

    public decimal CompletionPercent { get; set; }
}
