using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IRequestWorkflowService
{
    Task<RequestOverviewViewModel> GetRequestsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<RequestOverviewViewModel> GetRequestsForDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);

    Task<RequestOverviewViewModel> GetPendingApprovalsAsync(int departmentId, CancellationToken cancellationToken = default);

    Task<RequestOverviewViewModel> GetPendingAssessmentsAsync(CancellationToken cancellationToken = default);

    Task<SoftwareRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RequestDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateRequestAsync(RequestCreateViewModel model, int userId, CancellationToken cancellationToken = default);

    Task ApproveAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default);

    Task RejectAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default);

    Task AssessAsync(RequestAssessmentInputModel model, int assessorUserId, CancellationToken cancellationToken = default);
}
