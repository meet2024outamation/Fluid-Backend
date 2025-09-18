using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.User;
using System.Security.Claims;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class GetAccessibleTenants : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<AccessibleTenantsResponse>
{
    private readonly IManageUserService _manageUserService;

    public GetAccessibleTenants(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpGet("accessible-tenants")]
    [SwaggerOperation(
        Summary = "Get accessible tenants and projects",
        Description = "Retrieves all tenants and projects accessible to the current authenticated user based on their role assignments",
        OperationId = "User.GetAccessibleTenants",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "Accessible tenants and projects retrieved successfully", typeof(AccessibleTenantsResponse))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<AccessibleTenantsResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        // Extract user identifier from JWT claims and clean up domain prefixes
        var userIdentifier = User.FindFirstValue("preferred_username")?.Replace("live.com#", "") 
                          ?? User.FindFirstValue("upn")?.Replace("live.com#", "")
                          ?? User.FindFirstValue("unique_name")?.Replace("live.com#", "")
                          ?? User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userIdentifier))
        {
            return BadRequest("Unable to determine user identity from token claims.");
        }

        var result = await _manageUserService.GetAccessibleTenantsByIdentifierAsync(userIdentifier);
        return result.ToActionResult();
    }
}