using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.ViewModels;

public class ProjectTeamAssignmentInputModel
{
    public int RequestId { get; set; }

    public string TalepBasligi { get; set; } = string.Empty;

    [Display(Name = "Ekip Lideri")]
    public int? LeadUserId { get; set; }

    [Display(Name = "Geliştiriciler")]
    public List<int> DeveloperIds { get; set; } = new();

    [Display(Name = "Test Yazılımcıları")]
    public List<int> TesterIds { get; set; } = new();

    public IReadOnlyCollection<SelectableUserViewModel> Adaylar { get; set; } = Array.Empty<SelectableUserViewModel>();
}
