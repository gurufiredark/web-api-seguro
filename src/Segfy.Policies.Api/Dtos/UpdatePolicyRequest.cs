using Segfy.Policies.Api.Models;

namespace Segfy.Policies.Api.Dtos;

public sealed class UpdatePolicyRequest
{
    public string? InsuredDocument { get; init; }
    public string? VehiclePlate { get; init; }
    public decimal MonthlyPremium { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public PolicyStatus Status { get; init; }
}
