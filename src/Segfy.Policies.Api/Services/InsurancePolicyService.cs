using Microsoft.EntityFrameworkCore;
using Segfy.Policies.Api.Data;
using Segfy.Policies.Api.Dtos;
using Segfy.Policies.Api.Models;

namespace Segfy.Policies.Api.Services;

public sealed class InsurancePolicyService(
    PoliciesDbContext dbContext,
    IPolicyNumberGenerator policyNumberGenerator,
    IDateTimeProvider dateTimeProvider) : IInsurancePolicyService
{
    public async Task<IReadOnlyList<PolicyResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Policies
            .AsNoTracking()
            .OrderByDescending(policy => policy.CreatedAtUtc)
            .Select(policy => ToResponse(policy))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await dbContext.Policies
            .AsNoTracking()
            .FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

        return policy is null ? null : ToResponse(policy);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetExpiringWithinNext30DaysAsync(CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.Today;
        var limit = today.AddDays(30);

        return await dbContext.Policies
            .AsNoTracking()
            .Where(policy => policy.EndDate >= today && policy.EndDate <= limit)
            .OrderBy(policy => policy.EndDate)
            .ThenBy(policy => policy.PolicyNumber)
            .Select(policy => ToResponse(policy))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        Validate(request.InsuredDocument, request.VehiclePlate, request.MonthlyPremium, request.StartDate, request.EndDate);

        var policy = new InsurancePolicy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = await policyNumberGenerator.GenerateAsync(cancellationToken),
            InsuredDocument = NormalizeDocument(request.InsuredDocument),
            VehiclePlate = NormalizePlate(request.VehiclePlate),
            MonthlyPremium = request.MonthlyPremium,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAtUtc = dateTimeProvider.UtcNow
        };

        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(policy);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        Validate(request.InsuredDocument, request.VehiclePlate, request.MonthlyPremium, request.StartDate, request.EndDate);

        var policy = await dbContext.Policies.FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);
        if (policy is null)
        {
            return false;
        }

        policy.InsuredDocument = NormalizeDocument(request.InsuredDocument);
        policy.VehiclePlate = NormalizePlate(request.VehiclePlate);
        policy.MonthlyPremium = request.MonthlyPremium;
        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.Status = request.Status;
        policy.UpdatedAtUtc = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await dbContext.Policies.FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);
        if (policy is null)
        {
            return false;
        }

        dbContext.Policies.Remove(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(
        string? insuredDocument,
        string? vehiclePlate,
        decimal monthlyPremium,
        DateOnly startDate,
        DateOnly endDate)
    {
        var errors = new Dictionary<string, List<string>>();
        var document = NormalizeDocument(insuredDocument);
        var plate = NormalizePlate(vehiclePlate);

        if (document.Length is not (11 or 14))
        {
            AddError(errors, nameof(CreatePolicyRequest.InsuredDocument), "Informe um CPF com 11 digitos ou CNPJ com 14 digitos.");
        }

        if (plate.Length != 7 || plate.Any(character => !char.IsLetterOrDigit(character)))
        {
            AddError(errors, nameof(CreatePolicyRequest.VehiclePlate), "Informe uma placa com 7 caracteres alfanumericos.");
        }

        if (monthlyPremium <= 0)
        {
            AddError(errors, nameof(CreatePolicyRequest.MonthlyPremium), "O valor do premio mensal deve ser maior que zero.");
        }

        if (startDate == default)
        {
            AddError(errors, nameof(CreatePolicyRequest.StartDate), "Informe a data de inicio da vigencia.");
        }

        if (endDate == default)
        {
            AddError(errors, nameof(CreatePolicyRequest.EndDate), "Informe a data de termino da vigencia.");
        }

        if (startDate != default && endDate != default && endDate < startDate)
        {
            AddError(errors, nameof(CreatePolicyRequest.EndDate), "A data de termino deve ser maior ou igual a data de inicio.");
        }

        if (errors.Count > 0)
        {
            throw new PolicyValidationException(errors.ToDictionary(
                error => error.Key,
                error => error.Value.ToArray()));
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    private static string NormalizeDocument(string? value)
    {
        return new string((value ?? string.Empty)
            .Where(char.IsDigit)
            .ToArray());
    }

    private static string NormalizePlate(string? value)
    {
        return new string((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static PolicyResponse ToResponse(InsurancePolicy policy)
    {
        return new PolicyResponse(
            policy.Id,
            policy.PolicyNumber,
            policy.InsuredDocument,
            policy.VehiclePlate,
            policy.MonthlyPremium,
            policy.StartDate,
            policy.EndDate,
            policy.Status,
            policy.CreatedAtUtc,
            policy.UpdatedAtUtc);
    }
}
