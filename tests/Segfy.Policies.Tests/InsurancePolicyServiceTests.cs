using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segfy.Policies.Api.Data;
using Segfy.Policies.Api.Dtos;
using Segfy.Policies.Api.Models;
using Segfy.Policies.Api.Services;

namespace Segfy.Policies.Tests;

public sealed class InsurancePolicyServiceTests
{
    [Fact]
    public async Task CreateAsync_GeneratesPolicyNumberAndNormalizesInput()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Policies.Add(new InsurancePolicy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = "SEG-2026-0007",
            InsuredDocument = "12345678901",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 120,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PolicyStatus.Ativa,
            CreatedAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        await dbContext.SaveChangesAsync();

        var clock = new FakeDateTimeProvider(
            new DateOnly(2026, 6, 30),
            new DateTime(2026, 6, 30, 15, 0, 0, DateTimeKind.Utc));
        var service = CreateService(dbContext, clock);

        var response = await service.CreateAsync(new CreatePolicyRequest
        {
            InsuredDocument = "123.456.789-01",
            VehiclePlate = "abc-1d23",
            MonthlyPremium = 199.90m,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2027, 7, 1),
            Status = PolicyStatus.Ativa
        }, CancellationToken.None);

        Assert.Equal("SEG-2026-0008", response.PolicyNumber);
        Assert.Equal("12345678901", response.InsuredDocument);
        Assert.Equal("ABC1D23", response.VehiclePlate);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidPolicy()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<PolicyValidationException>(() =>
            service.CreateAsync(new CreatePolicyRequest
            {
                InsuredDocument = "123",
                VehiclePlate = "A1",
                MonthlyPremium = 0,
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2026, 7, 1),
                Status = PolicyStatus.Ativa
            }, CancellationToken.None));

        Assert.Contains(nameof(CreatePolicyRequest.InsuredDocument), exception.Errors.Keys);
        Assert.Contains(nameof(CreatePolicyRequest.VehiclePlate), exception.Errors.Keys);
        Assert.Contains(nameof(CreatePolicyRequest.MonthlyPremium), exception.Errors.Keys);
        Assert.Contains(nameof(CreatePolicyRequest.EndDate), exception.Errors.Keys);
    }

    [Fact]
    public async Task GetExpiringWithinNext30DaysAsync_ReturnsPoliciesInDateWindow()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var today = new DateOnly(2026, 6, 30);
        dbContext.Policies.AddRange(
            CreatePolicy("SEG-2026-0001", today.AddDays(10)),
            CreatePolicy("SEG-2026-0002", today.AddDays(30)),
            CreatePolicy("SEG-2026-0003", today.AddDays(31)),
            CreatePolicy("SEG-2026-0004", today.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new FakeDateTimeProvider(today, DateTime.UtcNow));

        var result = await service.GetExpiringWithinNext30DaysAsync(CancellationToken.None);

        Assert.Equal(["SEG-2026-0001", "SEG-2026-0002"], result.Select(policy => policy.PolicyNumber));
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static PoliciesDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PoliciesDbContext>()
            .UseSqlite(connection)
            .Options;

        return new PoliciesDbContext(options);
    }

    private static InsurancePolicyService CreateService(
        PoliciesDbContext dbContext,
        IDateTimeProvider? clock = null)
    {
        clock ??= new FakeDateTimeProvider(
            new DateOnly(2026, 6, 30),
            new DateTime(2026, 6, 30, 15, 0, 0, DateTimeKind.Utc));

        return new InsurancePolicyService(
            dbContext,
            new PolicyNumberGenerator(dbContext, clock),
            clock);
    }

    private static InsurancePolicy CreatePolicy(string policyNumber, DateOnly endDate)
    {
        return new InsurancePolicy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = policyNumber,
            InsuredDocument = "12345678901",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 150,
            StartDate = endDate.AddYears(-1),
            EndDate = endDate,
            Status = PolicyStatus.Ativa,
            CreatedAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class FakeDateTimeProvider(DateOnly today, DateTime utcNow) : IDateTimeProvider
    {
        public DateOnly Today { get; } = today;
        public DateTime UtcNow { get; } = utcNow;
    }
}
