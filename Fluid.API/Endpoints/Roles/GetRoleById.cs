using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Role;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Roles;

[Route("api/roles")]
[ApiController]
public class GetRoleById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<RoleDto>
{
    private readonly IRoleService _roleService;

    public GetRoleById(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("{id:int}")]
    [AuthorizePermission(ApplicationPermissions.ViewRoles)]
    [SwaggerOperation(
        Summary = "Get role by ID",
        Description = "Retrieves a specific role with its assigned permissions. Requires ViewRoles permission.",
        OperationId = "GetRoleById",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(200, "Success", typeof(RoleDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    [SwaggerResponse(404, "Role not found")]
    public override async Task<ActionResult<RoleDto>> HandleAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var result = await _roleService.GetRoleByIdAsync(id);
        return result.ToActionResult();
    }
}