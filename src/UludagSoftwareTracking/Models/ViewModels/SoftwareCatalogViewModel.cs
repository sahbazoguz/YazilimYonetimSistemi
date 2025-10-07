using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class SoftwareCatalogViewModel
{
    [Display(Name = "Yazılım Adı")]
    public string? AramaMetni { get; set; }

    [Display(Name = "Birim")]
    public int? BirimId { get; set; }

    public IReadOnlyCollection<Department> Birimler { get; init; } = Array.Empty<Department>();

    public IReadOnlyCollection<Software> Yazilimlar { get; init; } = Array.Empty<Software>();

    public IReadOnlyCollection<SoftwareCatalogItemViewModel> Kayitlar { get; init; } = Array.Empty<SoftwareCatalogItemViewModel>();
}

public class SoftwareCatalogItemViewModel
{
    public int Id { get; init; }

    public string Ad { get; init; } = string.Empty;

    public string? Aciklama { get; init; }

    public string? Kategori { get; init; }

    public string? BirimAdi { get; init; }

    public string? KullanimKilavuzuBaslik { get; init; }

    public string? KullanimKilavuzuUrl { get; init; }
}
