using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Role;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Roles;

[Route("api/roles")]
[ApiController]
public class GetRolesWithPermissions : EndpointBaseAsync.WithoutRequest.WithActionResult<List<RoleDto>>
{
    private readonly IRoleService _roleService;

    public GetRolesWithPermissions(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("detailed")]
    [AuthorizePermission(ApplicationPermissions.ViewRoles)]
    [SwaggerOperation(
        Summary = "Get all roles with detailed permissions",
        Description = "Retrieves all active roles with their detailed permission assignments. Requires ViewRoles permission.",
        OperationId = "GetRolesWithPermissions",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(200, "Success", typeof(List<RoleDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    public override async Task<ActionResult<List<RoleDto>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        // Get all roles first
        var rolesResult = await _roleService.GetAllRolesAsync();
        if (!rolesResult.IsSuccess)
        {
            // Convert the errors to the expected return type
            return BadRequest(new 
            { 
                errors = rolesResult.Errors,
                validationErrors = rolesResult.ValidationErrors,
                message = rolesResult.Errors?.FirstOrDefault() ?? "An error occurred"
            });
        }

        // Get detailed information for each role
        var detailedRoles = new List<RoleDto>();
        foreach (var roleListItem in rolesResult.Value!)
        {
            var detailedResult = await _roleService.GetRoleByIdAsync(roleListItem.Id);
            if (detailedResult.IsSuccess)
            {
                detailedRoles.Add(detailedResult.Value!);
            }
        }

        var result = SharedKernel.Result.Result<List<RoleDto>>.Success(detailedRoles, $"Retrieved {detailedRoles.Count} roles with permissions");
        return result.ToActionResult();
    }
}