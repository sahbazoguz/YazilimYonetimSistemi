using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UludagSoftwareTracking.Models.Entities;

public class SoftwareResponsibility
{
    public int Id { get; set; }

    public int SoftwareId { get; set; }

    public int UserId { get; set; }

    public SoftwareResponsibilityType ResponsibilityType { get; set; }

    [ForeignKey(nameof(SoftwareId))]
    public Software? Software { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserProfile? User { get; set; }
}
