using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AssessmentHistoryAdminViewModel
{
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? Baslangic { get; set; }

    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? Bitis { get; set; }

    [Display(Name = "Karar")]
    public AssessmentResult? Karar { get; set; }

    [Display(Name = "Aşama")]
    public AssessmentStage? Asama { get; set; }

    [Display(Name = "Değerlendirici")]
    public int? DegerlendiriciId { get; set; }

    public IReadOnlyCollection<SelectableUserViewModel> Degerlendiriciler { get; init; } = Array.Empty<SelectableUserViewModel>();

    public IReadOnlyCollection<AssessmentHistoryAdminItemViewModel> Kayitlar { get; init; } = Array.Empty<AssessmentHistoryAdminItemViewModel>();
}
