namespace Segfy.Policies.Api.Services;

public interface IPolicyNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}
