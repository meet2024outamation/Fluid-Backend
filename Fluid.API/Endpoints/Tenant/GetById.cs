using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Tenant;

[Route("api/tenants")]
//[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class GetById : EndpointBaseAsync.WithRequest<string>.WithActionResult<Entities.IAM.Tenant>
{
    private readonly ITenantService _tenantService;

    public GetById(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get tenant by ID",
        Description = "Retrieves a specific tenant by its ID. Requires Product Owner role.",
        OperationId = "GetTenantById",
        Tags = new[] { "Tenants" })]
    [SwaggerResponse(200, "Success", typeof(Entities.IAM.Tenant))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Tenant not found")]
    public async override Task<ActionResult<Entities.IAM.Tenant>> HandleAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);
        return result.ToActionResult();
    }
}