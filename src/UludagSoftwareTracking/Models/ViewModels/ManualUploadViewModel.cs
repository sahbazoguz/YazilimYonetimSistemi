using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class ManualUploadViewModel
{
    [Required]
    [Display(Name = "Yazılım")]
    public int SoftwareId { get; set; }

    [Required]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Dosya Yolu")]
    public string? FilePath { get; set; }

    [Display(Name = "Kılavuz Türü")]
    public ManualType ManualType { get; set; }

    [Display(Name = "Versiyon")]
    public string? Version { get; set; }

    [Display(Name = "Değişiklik Notu")]
    public string? ChangeLog { get; set; }

    public IReadOnlyCollection<Software> Yazilimlar { get; set; } = Array.Empty<Software>();
}
