namespace Segfy.Policies.Api.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public DateTime UtcNow => DateTime.UtcNow;
}
