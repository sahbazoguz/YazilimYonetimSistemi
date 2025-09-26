using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AssessmentHistoryViewModel
{
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? Baslangic { get; set; }

    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? Bitis { get; set; }

    [Display(Name = "Karar")]
    public AssessmentResult? Karar { get; set; }

    public IReadOnlyCollection<AssessmentHistoryItemViewModel> Kayitlar { get; init; } = Array.Empty<AssessmentHistoryItemViewModel>();
}
