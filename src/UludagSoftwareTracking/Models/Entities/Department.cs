using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class Department
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(25)]
    public string? PhoneNumber { get; set; }

    public ICollection<Software> Softwares { get; set; } = new List<Software>();

    public ICollection<UserProfile> Users { get; set; } = new List<UserProfile>();
}
