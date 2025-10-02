using System;
using System.Collections.Generic;

namespace UludagSoftwareTracking.Models.ViewModels;

public class ManualOverviewViewModel
{
    public IReadOnlyList<ManualOverviewItemViewModel> Yazilimlar { get; init; } = Array.Empty<ManualOverviewItemViewModel>();
}

public class ManualOverviewItemViewModel
{
    public int Id { get; init; }

    public string Ad { get; init; } = string.Empty;

    public string? BirimAdi { get; init; }

    public string? Aciklama { get; init; }

    public bool TeknikKilavuzYetkisi { get; init; }

    public bool KullanimKilavuzuYetkisi { get; init; }

    public ManualFileSummaryViewModel? TeknikKilavuz { get; init; }

    public ManualFileSummaryViewModel? KullanimKilavuzu { get; init; }
}

public class ManualFileSummaryViewModel
{
    public int Id { get; init; }

    public string Baslik { get; init; } = string.Empty;

    public string? DosyaYolu { get; init; }

    public string? Versiyon { get; init; }

    public DateTime YuklenmeZamani { get; init; }

    public string? Yukleyen { get; init; }

    public string? DegisimNotu { get; init; }
}
