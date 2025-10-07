using System;
using System.Collections.Generic;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class DashboardViewModel
{
    public string KullaniciAdi { get; init; } = string.Empty;

    public string Rol { get; init; } = string.Empty;

    public IReadOnlyCollection<DashboardTileViewModel> Kutucuklar { get; init; } = Array.Empty<DashboardTileViewModel>();

    public IReadOnlyCollection<RequestListItemViewModel> KritikTalepler { get; init; } = Array.Empty<RequestListItemViewModel>();

    public IReadOnlyCollection<Project> AktifProjeler { get; init; } = Array.Empty<Project>();

    public IReadOnlyCollection<Software> OneCikanYazilimlar { get; init; } = Array.Empty<Software>();

    public IReadOnlyCollection<SoftwareManual> SonKilavuzlar { get; init; } = Array.Empty<SoftwareManual>();
}
