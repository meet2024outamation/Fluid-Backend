using Fluid.Entities.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fluid.API.Authorization;

/// <summary>
/// Authorization handler that checks if user has the required permission(s)
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(FluidIAMDbContext iamContext, ILogger<PermissionAuthorizationHandler> logger)
    {
        _iamContext = iamContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
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

            _logger.LogDebug("Checking permission authorization for user identifier: {UserIdentifier}", userIdentifier);

            // Find user in database by email or Azure AD ID
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
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

            // Get all permissions for the user through their roles
            var userPermissions = user.UserRoleUsers
                .Where(ur => ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Where(rp => rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("User {UserId} has permissions: {UserPermissions}", 
                user.Id, 
                string.Join(", ", userPermissions));

            // Check if user has any of the required permissions
            bool hasRequiredPermission = requirement.RequiredPermissions
                .Any(requiredPermission => userPermissions.Contains(requiredPermission));

            if (hasRequiredPermission)
            {
                _logger.LogInformation("Permission authorization successful for user {UserId} ({Email}). Required permissions: {RequiredPermissions}",
                    user.Id, user.Email, string.Join(", ", requirement.RequiredPermissions));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Permission authorization failed for user {UserId} ({Email}). User permissions: {UserPermissions}, Required permissions: {RequiredPermissions}",
                    user.Id, user.Email,
                    string.Join(", ", userPermissions), 
                    string.Join(", ", requirement.RequiredPermissions));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission authorization");
            context.Fail();
        }
    }
}

/// <summary>
/// Authorization requirement that specifies required permissions
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string[] RequiredPermissions { get; }

    public PermissionRequirement(params string[] requiredPermissions)
    {
        RequiredPermissions = requiredPermissions ?? Array.Empty<string>();
    }
}