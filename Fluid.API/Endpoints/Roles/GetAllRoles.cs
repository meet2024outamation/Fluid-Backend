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
public class GetAllRoles : EndpointBaseAsync.WithoutRequest.WithActionResult<List<RoleListDto>>
{
    private readonly IRoleService _roleService;

    public GetAllRoles(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [AuthorizePermission(ApplicationPermissions.ViewRoles)]
    [SwaggerOperation(
        Summary = "Get all roles",
        Description = "Retrieves all active roles in the system with permission counts. Requires ViewRoles permission.",
        OperationId = "GetAllRoles",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(200, "Success", typeof(List<RoleListDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    public override async Task<ActionResult<List<RoleListDto>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = await _roleService.GetAllRolesAsync();
        return result.ToActionResult();
    }
}