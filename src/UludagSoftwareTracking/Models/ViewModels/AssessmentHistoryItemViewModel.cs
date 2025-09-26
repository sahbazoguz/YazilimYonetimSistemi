using System;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AssessmentHistoryItemViewModel
{
    public int AssessmentId { get; init; }

    public int RequestId { get; init; }

    public string TalepBasligi { get; init; } = string.Empty;

    public DateTime? Tarih { get; init; }

    public AssessmentResult Sonuc { get; init; }

    public string? KisaGerekce { get; init; }
}
