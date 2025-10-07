using System;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AssessmentHistoryAdminItemViewModel
{
    public int AssessmentId { get; init; }

    public int RequestId { get; init; }

    public string TalepBasligi { get; init; } = string.Empty;

    public string BirimAdi { get; init; } = string.Empty;

    public AssessmentStage Asama { get; init; }

    public AssessmentResult Sonuc { get; init; }

    public DateTime? Tarih { get; init; }

    public string Degerlendiren { get; init; } = string.Empty;
}
