using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class SoftwareManagementViewModel
{
    public IReadOnlyCollection<SoftwareManagementItemViewModel> Yazilimlar { get; init; } = Array.Empty<SoftwareManagementItemViewModel>();

    public SoftwareEditInputModel YeniYazilim { get; init; } = new();

    public IReadOnlyCollection<Department> Birimler { get; init; } = Array.Empty<Department>();

    public IReadOnlyCollection<SelectableUserViewModel> YazilimciAdaylari { get; init; } = Array.Empty<SelectableUserViewModel>();

    public IReadOnlyCollection<SelectableUserViewModel> BirimKullanicisiAdaylari { get; init; } = Array.Empty<SelectableUserViewModel>();

    public IReadOnlyCollection<SelectableUserViewModel> BirimYetkilisiAdaylari { get; init; } = Array.Empty<SelectableUserViewModel>();
}

public class SoftwareManagementItemViewModel
{
    public int Id { get; init; }

    public string Ad { get; init; } = string.Empty;

    public string? Aciklama { get; init; }

    public string? BirimAdi { get; init; }

    public string? DestekEposta { get; init; }

    public string? WebAdresi { get; init; }

    public IReadOnlyCollection<string> Yazilimcilar { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> BirimKullanicilari { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> BirimYetkilileri { get; init; } = Array.Empty<string>();

    public SoftwareEditInputModel Duzenleme { get; init; } = new();

    public SoftwareResponsibilityInputModel Sorumluluklar { get; init; } = new();
}

public class SoftwareEditInputModel
{
    [Required]
    [Display(Name = "Yazılım Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [MaxLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "Sorumlu Birim")]
    [Required(ErrorMessage = "Sorumlu birim seçilmelidir.")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Destek E-posta")]
    [EmailAddress]
    public string? SupportContact { get; set; }

    [Display(Name = "Web Adresi")]
    [Url]
    public string? WebsiteUrl { get; set; }

    public int? Id { get; set; }
}

public class SoftwareResponsibilityInputModel
{
    [Required]
    public int SoftwareId { get; set; }

    [Display(Name = "Yetkili Yazılımcılar")]
    public List<int> YazilimciIdleri { get; set; } = new();

    [Display(Name = "Yetkili Birim Kullanıcıları")]
    public List<int> BirimKullanicisiIdleri { get; set; } = new();

    [Display(Name = "Yetkili Birim Yetkilileri")]
    public List<int> BirimYetkilisiIdleri { get; set; } = new();
}
