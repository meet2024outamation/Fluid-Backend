using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Tenant;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Tenant;

[Route("api/tenants")]
//[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class Create : EndpointBaseAsync.WithRequest<CreateTenantRequest>.WithActionResult<Entities.IAM.Tenant>
{
    private readonly ITenantService _tenantService;

    public Create(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new tenant",
        Description = "Creates a new tenant with its own database. Requires Product Owner role.",
        OperationId = "CreateTenant",
        Tags = new[] { "Tenants" })]
    [SwaggerResponse(201, "Tenant created successfully", typeof(Entities.IAM.Tenant))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(409, "Tenant already exists")]
    public async override Task<ActionResult<Entities.IAM.Tenant>> HandleAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var result = await _tenantService.CreateTenantAsync(request);
        return result.ToActionResult();
    }


}