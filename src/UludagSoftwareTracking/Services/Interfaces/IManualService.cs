using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IManualService
{
    Task<IReadOnlyList<SoftwareManual>> GetManualsAsync(CancellationToken cancellationToken = default);

    Task<ManualUploadViewModel> GetManualUploadModelAsync(int? softwareId, CancellationToken cancellationToken = default);

    Task<int> SaveManualAsync(ManualUploadViewModel model, int userId, CancellationToken cancellationToken = default);
}
