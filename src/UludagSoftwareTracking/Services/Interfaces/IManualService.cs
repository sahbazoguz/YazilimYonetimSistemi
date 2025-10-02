using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IManualService
{
    Task<IReadOnlyList<SoftwareManual>> GetManualsAsync(int userId, CancellationToken cancellationToken = default);

    Task<ManualUploadViewModel> GetManualUploadModelAsync(int userId, int? softwareId, CancellationToken cancellationToken = default);

    Task<int> SaveManualAsync(ManualUploadViewModel model, int userId, CancellationToken cancellationToken = default);
}
