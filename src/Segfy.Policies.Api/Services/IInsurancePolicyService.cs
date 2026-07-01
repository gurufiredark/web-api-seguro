using Segfy.Policies.Api.Dtos;

namespace Segfy.Policies.Api.Services;

public interface IInsurancePolicyService
{
    Task<IReadOnlyList<PolicyResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<PolicyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyResponse>> GetExpiringWithinNext30DaysAsync(CancellationToken cancellationToken);
    Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
