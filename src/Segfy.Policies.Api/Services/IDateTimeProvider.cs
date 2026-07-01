namespace Segfy.Policies.Api.Services;

public interface IDateTimeProvider
{
    DateOnly Today { get; }
    DateTime UtcNow { get; }
}
