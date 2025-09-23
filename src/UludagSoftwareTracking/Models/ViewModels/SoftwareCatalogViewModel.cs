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

    [Display(Name = "Teknoloji")] 
    public string? Teknoloji { get; set; }

    public IReadOnlyCollection<Department> Birimler { get; init; } = Array.Empty<Department>();

    public IReadOnlyCollection<string> Teknolojiler { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<Software> Yazilimlar { get; init; } = Array.Empty<Software>();
}
