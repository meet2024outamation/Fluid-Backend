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

            // Get user identifier from claims and clean up domain prefixes
            var userIdentifier = context.User.FindFirstValue("preferred_username")?.Replace("live.com#", "")
                              ?? context.User.FindFirstValue("upn")?.Replace("live.com#", "")
                              ?? context.User.FindFirstValue("unique_name")?.Replace("live.com#", "")
                              ?? context.User.FindFirstValue(ClaimTypes.Email)
                              ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdentifier))
            {
                _logger.LogWarning("No user identifier found in claims");
                context.Fail();
                return;
            }

            _logger.LogDebug("Checking authorization for user identifier: {UserIdentifier}", userIdentifier);

            // Find user in database by email or Azure AD ID
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                .Where(u => (u.Email == userIdentifier || u.AzureAdId.Contains(userIdentifier)) && u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found in database with identifier: {UserIdentifier}", userIdentifier);
                context.Fail();
                return;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("User {UserId} is not active", user.Id);
                context.Fail();
                return;
            }

            // Get all user roles (including global roles with null tenant/project)
            var userRoles = user.UserRoleUsers
                .Where(ur => ur.Role.IsActive)
                .Select(ur => new { 
                    RoleName = ur.Role.Name, 
                    TenantId = ur.TenantId, 
                    ProjectId = ur.ProjectId 
                })
                .ToList();

            _logger.LogDebug("User {UserId} has roles: {UserRoles}", 
                user.Id, 
                string.Join(", ", userRoles.Select(r => $"{r.RoleName}(T:{r.TenantId},P:{r.ProjectId})")));

            // Check if user has any of the required roles
            bool hasRequiredRole = false;

            foreach (var requiredRole in requirement.AllowedRoles)
            {
                // Special handling for Product Owner - must have null tenant and project
                if (requiredRole == ApplicationRoles.ProductOwner)
                {
                    hasRequiredRole = userRoles.Any(ur => 
                        ur.RoleName.Equals(ApplicationRoles.ProductOwner, StringComparison.OrdinalIgnoreCase) &&
                        ur.TenantId == null && 
                        ur.ProjectId == null);
                    
                    if (hasRequiredRole)
                    {
                        _logger.LogInformation("User {UserId} has Product Owner role with global access", user.Id);
                        break;
                    }
                }
                // For other roles, check if user has the role regardless of tenant/project context
                else
                {
                    hasRequiredRole = userRoles.Any(ur => 
                        ur.RoleName.Equals(requiredRole, StringComparison.OrdinalIgnoreCase));
                    
                    if (hasRequiredRole)
                    {
                        _logger.LogInformation("User {UserId} has required role: {RequiredRole}", user.Id, requiredRole);
                        break;
                    }
                }
            }

            if (hasRequiredRole)
            {
                _logger.LogInformation("Authorization successful for user {UserId} ({Email}). Required roles: {RequiredRoles}",
                    user.Id, user.Email, string.Join(", ", requirement.AllowedRoles));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization failed for user {UserId} ({Email}). User roles: {UserRoles}, Required roles: {RequiredRoles}",
                    user.Id, user.Email,
                    string.Join(", ", userRoles.Select(r => r.RoleName)), 
                    string.Join(", ", requirement.AllowedRoles));
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
}