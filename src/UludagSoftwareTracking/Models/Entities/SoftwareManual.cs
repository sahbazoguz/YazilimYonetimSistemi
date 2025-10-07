using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class SoftwareManual
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public ManualType ManualType { get; set; }

    [StringLength(400)]
    public string? FilePath { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    public string? ChangeLog { get; set; }

    public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

    public int SoftwareId { get; set; }

    public Software? Software { get; set; }

    public int? UploadedByUserId { get; set; }

    public UserProfile? UploadedByUser { get; set; }
}
