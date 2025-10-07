using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface ISoftwareCatalogService
{
    Task<SoftwareCatalogViewModel> GetCatalogAsync(string? search, int? departmentId, CancellationToken cancellationToken = default);

    Task<SoftwareDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SoftwareManual>> GetManualsForSoftwareAsync(int softwareId, CancellationToken cancellationToken = default);
}
