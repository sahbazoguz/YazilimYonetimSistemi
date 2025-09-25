using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    public int RecipientUserId { get; set; }

    public UserProfile? RecipientUser { get; set; }
}
