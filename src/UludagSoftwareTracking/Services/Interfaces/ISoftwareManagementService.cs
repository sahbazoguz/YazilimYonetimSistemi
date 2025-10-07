using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface ISoftwareManagementService
{
    Task<SoftwareManagementViewModel> GetManagementViewModelAsync(CancellationToken cancellationToken = default);

    Task CreateSoftwareAsync(SoftwareEditInputModel model, int actingUserId, CancellationToken cancellationToken = default);

    Task UpdateSoftwareAsync(SoftwareEditInputModel model, CancellationToken cancellationToken = default);

    Task DeleteSoftwareAsync(int softwareId, CancellationToken cancellationToken = default);

    Task UpdateResponsibilitiesAsync(SoftwareResponsibilityInputModel model, CancellationToken cancellationToken = default);
}
