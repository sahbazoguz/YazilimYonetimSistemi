using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

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

    [Precision(18, 4)]
    public decimal CompletionPercent { get; set; }
}
