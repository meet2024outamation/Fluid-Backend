using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Tenant;

[Route("api/tenants")]
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class Delete : EndpointBaseAsync.WithRequest<string>.WithActionResult<bool>
{
    private readonly ITenantService _tenantService;

    public Delete(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Delete a tenant",
        Description = "Soft deletes a tenant by marking it as inactive. Requires Product Owner role.",
        OperationId = "DeleteTenant",
        Tags = new[] { "Tenants" })]
    [SwaggerResponse(200, "Tenant deleted successfully")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Tenant not found")]
    public async override Task<ActionResult<bool>> HandleAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _tenantService.DeleteTenantAsync(id);
        return result.ToActionResult();
    }
}