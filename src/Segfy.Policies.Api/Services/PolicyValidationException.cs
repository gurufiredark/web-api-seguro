namespace Segfy.Policies.Api.Services;

public sealed class PolicyValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Policy validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
