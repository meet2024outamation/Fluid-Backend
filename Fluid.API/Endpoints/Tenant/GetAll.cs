using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Tenant;

[Route("api/tenants")]
//[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class GetAll : EndpointBaseAsync.WithoutRequest.WithActionResult<IEnumerable<Entities.IAM.Tenant>>
{
    private readonly ITenantService _tenantService;

    public GetAll(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all active tenants",
        Description = "Retrieves a list of all active tenants from the IAM database. Requires Product Owner role.",
        OperationId = "GetAllTenants",
        Tags = new[] { "Tenants" })]
    [SwaggerResponse(200, "Success", typeof(IEnumerable<Entities.IAM.Tenant>))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    public async override Task<ActionResult<IEnumerable<Entities.IAM.Tenant>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = await _tenantService.GetAllTenantsAsync();
        return result.ToActionResult();
    }
}