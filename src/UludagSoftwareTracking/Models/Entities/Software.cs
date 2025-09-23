using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class Software
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(150)]
    public string? Category { get; set; }

    [StringLength(150)]
    public string? TechnologyStack { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    [StringLength(300)]
    public string? SupportContact { get; set; }

    [StringLength(300)]
    public string? WebsiteUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SoftwareManual> Manuals { get; set; } = new List<SoftwareManual>();
}
