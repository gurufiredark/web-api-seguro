namespace Segfy.Policies.Api.Models;

public sealed class InsurancePolicy
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string InsuredDocument { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public decimal MonthlyPremium { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PolicyStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
