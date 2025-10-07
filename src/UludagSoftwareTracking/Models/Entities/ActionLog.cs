using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class ActionLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public UserProfile? User { get; set; }

    [Required]
    [StringLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
