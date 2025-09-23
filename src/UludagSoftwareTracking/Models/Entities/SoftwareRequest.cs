using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class SoftwareRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public RequestPriority Priority { get; set; } = RequestPriority.Orta;

    public RequestStatus Status { get; set; } = RequestStatus.OnayBekleniyor;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DesiredCompletionDate { get; set; }

    public DateTime? AssessmentDueDate { get; set; }

    [StringLength(200)]
    public string? RejectionReason { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int RequestedByUserId { get; set; }

    public UserProfile? RequestedByUser { get; set; }

    public int? ExistingSoftwareId { get; set; }

    public Software? ExistingSoftware { get; set; }

    public ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();

    public ICollection<RequestAssessment> Assessments { get; set; } = new List<RequestAssessment>();

    public Project? Project { get; set; }
}
