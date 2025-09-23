using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class RequestAssessment
{
    public int Id { get; set; }

    public AssessmentResult Result { get; set; } = AssessmentResult.Beklemede;

    public DateTime? AssessedOn { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public int RequestId { get; set; }

    public SoftwareRequest? Request { get; set; }

    public int? AssessedByUserId { get; set; }

    public UserProfile? AssessedByUser { get; set; }

    public int? ExistingSoftwareId { get; set; }

    public Software? ExistingSoftware { get; set; }
}
