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

    Task<RequestOverviewViewModel> GetPendingAssessmentsAsync(AssessmentStage stage, CancellationToken cancellationToken = default);

    Task<RequestOverviewViewModel> GetPendingBaskanApprovalsAsync(CancellationToken cancellationToken = default);

    Task<SoftwareRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RequestDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateRequestAsync(RequestCreateViewModel model, int userId, CancellationToken cancellationToken = default);

    Task ApproveAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default);

    Task RejectAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default);

    Task AssessAsync(RequestAssessmentInputModel model, int assessorUserId, UserRole assessorRole, CancellationToken cancellationToken = default);

    Task UpdateAlgorithmAsync(int requestId, int userId, string algorithmNotes, CancellationToken cancellationToken = default);

    Task UpdateGuidesAsync(int requestId, int userId, string? technicalGuidePath, string? userGuidePath, CancellationToken cancellationToken = default);

    Task MarkDevelopmentCompletedAsync(int requestId, int userId, CancellationToken cancellationToken = default);

    Task ConfirmTestingAsync(int requestId, int userId, CancellationToken cancellationToken = default);

    Task AddDiscussionMessageAsync(int requestId, int userId, string message, CancellationToken cancellationToken = default);
}
