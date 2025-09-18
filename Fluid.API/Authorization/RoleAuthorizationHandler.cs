using Fluid.Entities.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fluid.API.Authorization;

/// <summary>
/// Authorization handler that checks if user has the required role
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<RoleAuthorizationHandler> _logger;

    public RoleAuthorizationHandler(FluidIAMDbContext iamContext, ILogger<RoleAuthorizationHandler> logger)
    {
        _iamContext = iamContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User is not authenticated");
                context.Fail();
                return;
            }

            // Get user identifier from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var azureAdIdClaim = context.User.FindFirst("preferred_username")?.Value
                               ?? context.User.FindFirst("upn")?.Value
                               ?? context.User.FindFirst("unique_name")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) && string.IsNullOrEmpty(azureAdIdClaim))
            {
                _logger.LogWarning("No user identifier found in claims");
                context.Fail();
                return;
            }

            // Find user in database
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    (userIdClaim != null && u.Id.ToString() == userIdClaim) ||
                    (azureAdIdClaim != null && u.AzureAdId == azureAdIdClaim));

            if (user == null)
            {
                _logger.LogWarning("User not found in database. UserIdClaim: {UserIdClaim}, AzureAdIdClaim: {AzureAdIdClaim}",
                    userIdClaim, azureAdIdClaim);
                context.Fail();
                return;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("User {UserId} is not active", user.Id);
                context.Fail();
                return;
            }

            // Check if user has any of the required roles
            var userRoles = user.UserRoleUsers
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role.Name)
                .ToList();

            bool hasRequiredRole = requirement.AllowedRoles.Any(requiredRole =>
                userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));

            if (hasRequiredRole)
            {
                _logger.LogInformation("User {UserId} has required role. User roles: {UserRoles}, Required roles: {RequiredRoles}",
                    user.Id, string.Join(", ", userRoles), string.Join(", ", requirement.AllowedRoles));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User {UserId} does not have required role. User roles: {UserRoles}, Required roles: {RequiredRoles}",
                    user.Id, string.Join(", ", userRoles), string.Join(", ", requirement.AllowedRoles));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during role authorization");
            context.Fail();
        }
    }
}

/// <summary>
/// Authorization requirement that specifies allowed roles
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles ?? Array.Empty<string>();
    }
};