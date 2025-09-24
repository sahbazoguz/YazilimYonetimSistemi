using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YazilimYonetimSistemi.Web.Models;

public class Department
{
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserProfile> Users { get; set; } = new List<UserProfile>();
}
