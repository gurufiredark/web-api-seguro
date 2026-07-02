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

    [Fact]
    public async Task GetAllAsync_ReturnsPoliciesOrderedByCreatedAtDescending()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Policies.AddRange(
            CreatePolicy("SEG-2026-0001", new DateOnly(2026, 12, 31), createdAtUtc: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
            CreatePolicy("SEG-2026-0002", new DateOnly(2026, 12, 31), createdAtUtc: new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc)),
            CreatePolicy("SEG-2026-0003", new DateOnly(2026, 12, 31), createdAtUtc: new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(["SEG-2026-0002", "SEG-2026-0003", "SEG-2026-0001"], result.Select(policy => policy.PolicyNumber));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPolicyWhenFound()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var expectedPolicy = CreatePolicy("SEG-2026-0001", new DateOnly(2026, 12, 31));
        dbContext.Policies.AddRange(
            expectedPolicy,
            CreatePolicy("SEG-2026-0002", new DateOnly(2026, 12, 31)));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(expectedPolicy.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedPolicy.Id, result.Id);
        Assert.Equal("SEG-2026-0001", result.PolicyNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenPolicyDoesNotExist()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPolicyAndNormalizesInput()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var existingPolicy = CreatePolicy("SEG-2026-0001", new DateOnly(2026, 12, 31));
        dbContext.Policies.Add(existingPolicy);
        await dbContext.SaveChangesAsync();

        var clock = new FakeDateTimeProvider(
            new DateOnly(2026, 6, 30),
            new DateTime(2026, 7, 1, 10, 30, 0, DateTimeKind.Utc));
        var service = CreateService(dbContext, clock);

        var updated = await service.UpdateAsync(existingPolicy.Id, new UpdatePolicyRequest
        {
            InsuredDocument = "12.345.678/0001-95",
            VehiclePlate = "def-4g56",
            MonthlyPremium = 250.75m,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2027, 8, 1),
            Status = PolicyStatus.Cancelada
        }, CancellationToken.None);

        var savedPolicy = await dbContext.Policies.SingleAsync(policy => policy.Id == existingPolicy.Id);
        Assert.True(updated);
        Assert.Equal("12345678000195", savedPolicy.InsuredDocument);
        Assert.Equal("DEF4G56", savedPolicy.VehiclePlate);
        Assert.Equal(250.75m, savedPolicy.MonthlyPremium);
        Assert.Equal(new DateOnly(2026, 8, 1), savedPolicy.StartDate);
        Assert.Equal(new DateOnly(2027, 8, 1), savedPolicy.EndDate);
        Assert.Equal(PolicyStatus.Cancelada, savedPolicy.Status);
        Assert.Equal(clock.UtcNow, savedPolicy.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalseWhenPolicyDoesNotExist()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var service = CreateService(dbContext);

        var updated = await service.UpdateAsync(Guid.NewGuid(), new UpdatePolicyRequest
        {
            InsuredDocument = "12345678901",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 150,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PolicyStatus.Ativa
        }, CancellationToken.None);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPolicy()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var existingPolicy = CreatePolicy("SEG-2026-0001", new DateOnly(2026, 12, 31));
        dbContext.Policies.Add(existingPolicy);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var deleted = await service.DeleteAsync(existingPolicy.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(dbContext.Policies);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenPolicyDoesNotExist()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var service = CreateService(dbContext);

        var deleted = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(deleted);
    }

    [Fact]
    public async Task CreateAsync_AcceptsCnpjDocument()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var service = CreateService(dbContext);

        var response = await service.CreateAsync(new CreatePolicyRequest
        {
            InsuredDocument = "12.345.678/0001-95",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 300,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PolicyStatus.Ativa
        }, CancellationToken.None);

        Assert.Equal("12345678000195", response.InsuredDocument);
    }

    [Fact]
    public async Task PolicyNumberGenerator_StartsSequenceAtOneWhenThereAreNoPoliciesForYear()
    {
        using var connection = CreateOpenConnection();
        await using var dbContext = CreateContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var generator = new PolicyNumberGenerator(
            dbContext,
            new FakeDateTimeProvider(new DateOnly(2026, 6, 30), DateTime.UtcNow));

        var policyNumber = await generator.GenerateAsync(CancellationToken.None);

        Assert.Equal("SEG-2026-0001", policyNumber);
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

    private static InsurancePolicy CreatePolicy(
        string policyNumber,
        DateOnly endDate,
        DateTime? createdAtUtc = null)
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
            CreatedAtUtc = createdAtUtc ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class FakeDateTimeProvider(DateOnly today, DateTime utcNow) : IDateTimeProvider
    {
        public DateOnly Today { get; } = today;
        public DateTime UtcNow { get; } = utcNow;
    }
}
