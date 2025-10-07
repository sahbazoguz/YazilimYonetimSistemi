using System;
using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.Entities;

public class RequestDiscussionMessage
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public SoftwareRequest? Request { get; set; }

    public int SenderId { get; set; }

    public UserProfile? Sender { get; set; }

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime PostedOn { get; set; } = DateTime.UtcNow;
}
