using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(UserProfile? user, CancellationToken cancellationToken = default);
}
