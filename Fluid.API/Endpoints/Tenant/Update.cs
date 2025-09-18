using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Tenant;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Tenant;

[Route("api/tenants")]
//[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class Update : EndpointBaseAsync.WithRequest<UpdateTenantRequest>.WithActionResult<Entities.IAM.Tenant>
{
    private readonly ITenantService _tenantService;

    public Update(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Update a tenant",
        Description = "Updates an existing tenant. Requires Product Owner role.",
        OperationId = "UpdateTenant",
        Tags = new[] { "Tenants" })]
    [SwaggerResponse(200, "Tenant updated successfully", typeof(Entities.IAM.Tenant))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Tenant not found")]
    [SwaggerResponse(409, "Tenant identifier already exists")]
    public async override Task<ActionResult<Entities.IAM.Tenant>> HandleAsync(UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tenant = new Entities.IAM.Tenant
        {
            Id = request.Id,
            Identifier = request.Identifier,
            Name = request.Name,
            Description = request.Description,
            ConnectionString = request.ConnectionString,
            DatabaseName = request.DatabaseName,
            Properties = request.Properties
        };

        var result = await _tenantService.UpdateTenantAsync(tenant);
        return result.ToActionResult();
    }
}
