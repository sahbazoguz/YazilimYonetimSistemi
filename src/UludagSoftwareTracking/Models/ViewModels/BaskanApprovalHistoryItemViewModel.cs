using System;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class BaskanApprovalHistoryItemViewModel
{
    public int AssessmentId { get; init; }

    public int RequestId { get; init; }

    public string TalepBasligi { get; init; } = string.Empty;

    public string BirimAdi { get; init; } = string.Empty;

    public string Durum { get; init; } = string.Empty;

    public AssessmentResult Karar { get; init; }

    public DateTime? KararTarihi { get; init; }

    public string ProjeDurumu { get; init; } = string.Empty;

    public string Lider { get; init; } = string.Empty;

    public string Gelistiriciler { get; init; } = string.Empty;

    public string TestEkibi { get; init; } = string.Empty;

    public bool ProjeVar { get; init; }
}
