using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<Department> CreateAsync(Department department, CancellationToken cancellationToken = default);

    Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Department department, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
