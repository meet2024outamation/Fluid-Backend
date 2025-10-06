using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class Me : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<UserMeResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IManageUserService _manageUserService;

    public Me(ICurrentUserService currentUserService, IManageUserService mangeUserService)
    {
        _currentUserService = currentUserService;
        _manageUserService = mangeUserService;
    }

    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get current user information",
        Description = @"Retrieves the current authenticated user's information with context-scoped roles and permissions.
        
        Scoping Rules:
        - ProductOwner: Returns global roles and permissions (ignores tenant/project context)
        - TenantAdmin: Returns tenant-scoped roles and permissions (requires X-Tenant-Id header)
        - Other roles (Keying, QC): Returns project-scoped roles and permissions (requires X-Tenant-Id header and projectId query parameter)",
        OperationId = "User.Me",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "Current user information with context-scoped roles and permissions", typeof(UserMeResponse))]
    [SwaggerResponse(400, "Missing required context parameters")]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<UserMeResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        // Get context parameters
        var tenantId = HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var projectIdParam = HttpContext.Request.Headers["X-Project-Id"].FirstOrDefault();
        int? projectId = null;

        if (int.TryParse(projectIdParam, out var parsedProjectId))
        {
            projectId = parsedProjectId;
        }

        var result = await _manageUserService.GetCurrentUserAsync(currentUserId, tenantId, projectId);
        return result.ToActionResult();
    }
}