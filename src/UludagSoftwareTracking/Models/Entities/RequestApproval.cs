using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class RequestApproval
{
    public int Id { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Beklemede;

    public DateTime? DecidedAt { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    public int RequestId { get; set; }

    public SoftwareRequest? Request { get; set; }

    public int? ApprovedByUserId { get; set; }

    public UserProfile? ApprovedByUser { get; set; }
}
