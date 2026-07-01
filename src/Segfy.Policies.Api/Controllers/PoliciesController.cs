using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Segfy.Policies.Api.Dtos;
using Segfy.Policies.Api.Services;

namespace Segfy.Policies.Api.Controllers;

[ApiController]
[Route("api/policies")]
public sealed class PoliciesController(IInsurancePolicyService policyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicyResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var policies = await policyService.GetAllAsync(cancellationToken);
        return Ok(policies);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var policy = await policyService.GetByIdAsync(id, cancellationToken);
        return policy is null ? NotFound() : Ok(policy);
    }

    [HttpGet("expiring-soon")]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicyResponse>>> GetExpiringSoon(CancellationToken cancellationToken)
    {
        var policies = await policyService.GetExpiringWithinNext30DaysAsync(cancellationToken);
        return Ok(policies);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PolicyResponse>> Create(
        CreatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var policy = await policyService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
        }
        catch (PolicyValidationException exception)
        {
            return ValidationProblem(ToModelState(exception));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await policyService.UpdateAsync(id, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (PolicyValidationException exception)
        {
            return ValidationProblem(ToModelState(exception));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await policyService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static ModelStateDictionary ToModelState(PolicyValidationException exception)
    {
        var modelState = new ModelStateDictionary();

        foreach (var error in exception.Errors)
        {
            foreach (var message in error.Value)
            {
                modelState.AddModelError(error.Key, message);
            }
        }

        return modelState;
    }
}
