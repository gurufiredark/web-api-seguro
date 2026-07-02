using Microsoft.AspNetCore.Mvc;
using Segfy.Policies.Api.Controllers;
using Segfy.Policies.Api.Dtos;
using Segfy.Policies.Api.Models;
using Segfy.Policies.Api.Services;

namespace Segfy.Policies.Tests;

public sealed class PoliciesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithPolicies()
    {
        var policies = new[] { CreateResponse(Guid.NewGuid()), CreateResponse(Guid.NewGuid()) };
        var controller = new PoliciesController(new StubPolicyService { AllPolicies = policies });

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(policies, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenPolicyExists()
    {
        var policy = CreateResponse(Guid.NewGuid());
        var controller = new PoliciesController(new StubPolicyService { PolicyById = policy });

        var result = await controller.GetById(policy.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(policy, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenPolicyDoesNotExist()
    {
        var controller = new PoliciesController(new StubPolicyService());

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetExpiringSoon_ReturnsOkWithPolicies()
    {
        var policies = new[] { CreateResponse(Guid.NewGuid()) };
        var controller = new PoliciesController(new StubPolicyService { ExpiringPolicies = policies });

        var result = await controller.GetExpiringSoon(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(policies, okResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWhenRequestIsValid()
    {
        var policy = CreateResponse(Guid.NewGuid());
        var controller = new PoliciesController(new StubPolicyService { CreatedPolicy = policy });

        var result = await controller.Create(new CreatePolicyRequest
        {
            InsuredDocument = "12345678901",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 150,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PolicyStatus.Ativa
        }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(PoliciesController.GetById), createdResult.ActionName);
        Assert.Same(policy, createdResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblemWhenServiceRejectsRequest()
    {
        var controller = new PoliciesController(new StubPolicyService
        {
            CreateException = new PolicyValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreatePolicyRequest.InsuredDocument)] = ["Informe um CPF com 11 digitos ou CNPJ com 14 digitos."]
            })
        });

        var result = await controller.Create(new CreatePolicyRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(CreatePolicyRequest.InsuredDocument), problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Update_ReturnsNoContentWhenPolicyIsUpdated()
    {
        var controller = new PoliciesController(new StubPolicyService { UpdateResult = true });

        var result = await controller.Update(Guid.NewGuid(), CreateUpdateRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenPolicyDoesNotExist()
    {
        var controller = new PoliciesController(new StubPolicyService { UpdateResult = false });

        var result = await controller.Update(Guid.NewGuid(), CreateUpdateRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsValidationProblemWhenServiceRejectsRequest()
    {
        var controller = new PoliciesController(new StubPolicyService
        {
            UpdateException = new PolicyValidationException(new Dictionary<string, string[]>
            {
                [nameof(UpdatePolicyRequest.VehiclePlate)] = ["Informe uma placa com 7 caracteres alfanumericos."]
            })
        });

        var result = await controller.Update(Guid.NewGuid(), CreateUpdateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(UpdatePolicyRequest.VehiclePlate), problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenPolicyIsDeleted()
    {
        var controller = new PoliciesController(new StubPolicyService { DeleteResult = true });

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenPolicyDoesNotExist()
    {
        var controller = new PoliciesController(new StubPolicyService { DeleteResult = false });

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static UpdatePolicyRequest CreateUpdateRequest()
    {
        return new UpdatePolicyRequest
        {
            InsuredDocument = "12345678901",
            VehiclePlate = "ABC1D23",
            MonthlyPremium = 150,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PolicyStatus.Ativa
        };
    }

    private static PolicyResponse CreateResponse(Guid id)
    {
        return new PolicyResponse(
            id,
            "SEG-2026-0001",
            "12345678901",
            "ABC1D23",
            150,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            PolicyStatus.Ativa,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            null);
    }

    private sealed class StubPolicyService : IInsurancePolicyService
    {
        public IReadOnlyList<PolicyResponse> AllPolicies { get; init; } = [];
        public IReadOnlyList<PolicyResponse> ExpiringPolicies { get; init; } = [];
        public PolicyResponse? PolicyById { get; init; }
        public PolicyResponse? CreatedPolicy { get; init; }
        public PolicyValidationException? CreateException { get; init; }
        public PolicyValidationException? UpdateException { get; init; }
        public bool UpdateResult { get; init; }
        public bool DeleteResult { get; init; }

        public Task<IReadOnlyList<PolicyResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(AllPolicies);
        }

        public Task<PolicyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(PolicyById);
        }

        public Task<IReadOnlyList<PolicyResponse>> GetExpiringWithinNext30DaysAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ExpiringPolicies);
        }

        public Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(CreatedPolicy ?? CreateResponse(Guid.NewGuid()));
        }

        public Task<bool> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(DeleteResult);
        }
    }
}
