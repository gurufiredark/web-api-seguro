using Segfy.Policies.Api.Models;

namespace Segfy.Policies.Api.Dtos;

public sealed record PolicyResponse(
    Guid Id,
    string PolicyNumber,
    string InsuredDocument,
    string VehiclePlate,
    decimal MonthlyPremium,
    DateOnly StartDate,
    DateOnly EndDate,
    PolicyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
