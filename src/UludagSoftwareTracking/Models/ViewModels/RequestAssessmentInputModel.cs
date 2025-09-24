using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class RequestAssessmentInputModel
{
    public int RequestId { get; set; }

    public AssessmentStage Stage { get; set; }

    [Display(Name = "Değerlendirme Sonucu")]
    public AssessmentResult Result { get; set; }

    [Display(Name = "Açıklama")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Var Olan Yazılım")]
    public int? ExistingSoftwareId { get; set; }

    public IReadOnlyCollection<Software> Yazilimlar { get; set; } = Array.Empty<Software>();
}
