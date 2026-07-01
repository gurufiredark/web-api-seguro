using Microsoft.EntityFrameworkCore;
using Segfy.Policies.Api.Data;

namespace Segfy.Policies.Api.Services;

public sealed class PolicyNumberGenerator(
    PoliciesDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IPolicyNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        var year = dateTimeProvider.Today.Year;
        var prefix = $"SEG-{year}-";

        var lastPolicyNumber = await dbContext.Policies
            .Where(policy => policy.PolicyNumber.StartsWith(prefix))
            .OrderByDescending(policy => policy.PolicyNumber)
            .Select(policy => policy.PolicyNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var lastSequence = 0;
        if (lastPolicyNumber is not null
            && lastPolicyNumber.Length > prefix.Length
            && int.TryParse(lastPolicyNumber[prefix.Length..], out var sequence))
        {
            lastSequence = sequence;
        }

        return $"{prefix}{lastSequence + 1:0000}";
    }
}
