using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.Entities.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;
    private readonly FluidIAMDbContext _iamContext;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserService> logger,
        FluidIAMDbContext iamContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _iamContext = iamContext;
    }

    public int GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return 0; // Return 0 for unauthenticated users
        }

        try
        {
            var principal = httpContext.User;

            // 1) Some systems inject app user id as integer in NameIdentifier
            var nameIdentifier = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameIdentifier) && int.TryParse(nameIdentifier, out var appUserId))
            {
                _logger.LogDebug("Current user ID from NameIdentifier (int) claim: {UserId}", appUserId);
                return appUserId;
            }

            // 2) Azure AD Object Id (oid) -> map to IAM.Users.AzureAdId
            //    Standard types: "oid" or "http://schemas.microsoft.com/identity/claims/objectidentifier"
            var objectId = principal.FindFirst("oid")?.Value
                         ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (!string.IsNullOrWhiteSpace(objectId))
            {
                var userByOid = _iamContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.AzureAdId == objectId);
                if (userByOid != null)
                {
                    _logger.LogDebug("Current user ID from Azure AD ObjectId (oid) lookup: {UserId}", userByOid.Id);
                    return userByOid.Id;
                }
            }

            // 3) Fall back to email-based lookup: preferred_username / emails / upn
            var email = principal.FindFirst("preferred_username")?.Value
                     ?? principal.FindFirst(ClaimTypes.Email)?.Value
                     ?? principal.FindFirst("emails")?.Value
                     ?? principal.FindFirst("upn")?.Value
                     ?? principal.FindFirst("unique_name")?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var userByEmail = _iamContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
                if (userByEmail != null)
                {
                    _logger.LogDebug("Current user ID from email lookup: {UserId} ({Email})", userByEmail.Id, email);
                    return userByEmail.Id;
                }
            }

            _logger.LogWarning(
                "Could not determine user ID from claims. NameIdentifier: {NameIdentifier}, OID: {Oid}, Email: {Email}",
                nameIdentifier, objectId, email);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID");
            return 0;
        }
    }

    public string GetCurrentUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return "Anonymous";
        }

        try
        {
            // Try to get user name from various claims
            var userName = httpContext.User.FindFirst("name")?.Value
                         ?? httpContext.User.FindFirst("given_name")?.Value
                         ?? httpContext.User.FindFirst("preferred_username")?.Value
                         ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                         ?? "Unknown User";

            _logger.LogDebug("Current user name: {UserName}", userName);
            return userName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user name");
            return "Unknown User";
        }
    }

    public string GetCurrentUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return string.Empty;
        }

        try
        {
            var email = httpContext.User.FindFirst("preferred_username")?.Value
                     ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                     ?? httpContext.User.FindFirst("emails")?.Value
                     ?? httpContext.User.FindFirst("upn")?.Value
                     ?? httpContext.User.FindFirst("unique_name")?.Value
                     ?? string.Empty;

            _logger.LogDebug("Current user email: {Email}", email);
            return email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user email");
            return string.Empty;
        }
    }

    public async Task<CurrentUserProfile?> GetCurrentUserProfileAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                _logger.LogWarning("Cannot get user profile for unauthenticated user");
                return null;
            }

            var user = await _iamContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found in database with ID: {UserId}", userId);
                return null;
            }

            return new CurrentUserProfile
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user profile");
            return null;
        }
    }

    public async Task<UserMeResponse?> GetCurrentUserWithContextAsync(string? tenantId, int? projectId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                _logger.LogWarning("Cannot get user context for unauthenticated user");
                return null;
            }

            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found in database with ID: {UserId}", userId);
                return null;
            }

            // Determine user's primary role type
            var isProductOwner = user.UserRoleUsers.Any(ur =>
                ur.Role.IsActive &&
                ur.Role.Name == ApplicationRoles.ProductOwner &&
                ur.TenantId == null &&
                ur.ProjectId == null);

            var isTenantAdmin = user.UserRoleUsers.Any(ur =>
                ur.Role.IsActive &&
                ur.Role.Name == ApplicationRoles.TenantAdmin &&
                ur.ProjectId == null);

            // Apply scoping rules based on role type
            List<Entities.IAM.UserRole> relevantRoles;
            //UserContextType contextType;
            string? currentTenantId = null;
            string? currentTenantName = null;
            int? currentProjectId = null;
            string? currentProjectName = null;

            if (isProductOwner)
            {
                // ProductOwner: Global scope only
                relevantRoles = user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive && ur.TenantId == null && ur.ProjectId == null)
                    .ToList();
                //contextType = UserContextType.Global;
            }
            else if (isTenantAdmin && !string.IsNullOrEmpty(tenantId))
            {
                // TenantAdmin: Tenant scope
                var tenant = await _iamContext.Tenants
                    .FirstOrDefaultAsync(t => t.Identifier == tenantId && t.IsActive);

                if (tenant == null)
                {
                    _logger.LogWarning("Tenant not found or inactive: {TenantId}", tenantId);
                    return null;
                }

                relevantRoles = user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive &&
                               (ur.TenantId == tenant.Id || ur.TenantId == null))
                    .ToList();
                //contextType = UserContextType.Tenant;
                currentTenantId = tenantId;
                currentTenantName = tenant.Name;
            }
            else if (!string.IsNullOrEmpty(tenantId) && projectId.HasValue)
            {
                // Project-scoped roles
                var tenant = await _iamContext.Tenants
                    .FirstOrDefaultAsync(t => t.Identifier == tenantId && t.IsActive);

                if (tenant == null)
                {
                    _logger.LogWarning("Tenant not found or inactive: {TenantId}", tenantId);
                    return null;
                }

                relevantRoles = user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive &&
                               ((ur.TenantId == tenant.Id && ur.ProjectId == projectId) ||
                                (ur.TenantId == null && ur.ProjectId == null)))
                    .ToList();

                //contextType = UserContextType.Project;
                currentTenantId = tenantId;
                currentTenantName = tenant.Name;
                currentProjectId = projectId;

                // Try to get project name
                try
                {
                    var tenantDbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                        .UseNpgsql(tenant.ConnectionString)
                        .Options;

                    using var tenantContext = new FluidDbContext(tenantDbOptions, tenant);
                    var project = await tenantContext.Projects
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == projectId);

                    currentProjectName = project?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve project name for ProjectId: {ProjectId}", projectId);
                }
            }
            else
            {
                // Default to all roles if context not specified
                relevantRoles = user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive)
                    .ToList();
                //contextType = UserContextType.Global;
            }

            // Get permissions from relevant roles
            var permissions = relevantRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Where(rp => rp.Permission.IsActive)
                .Select(rp => rp.Permission)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderBy(p => p.Name)
                .Select(p => new PermissionInfo
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description
                })
                .ToList();

            return new UserMeResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CurrentTenantId = currentTenantId,
                CurrentTenantName = currentTenantName,
                CurrentProjectId = currentProjectId,
                CurrentProjectName = currentProjectName,
                //ContextType = contextType,
                Roles = relevantRoles
                    .Select(ur => new UserRoleInfo
                    {
                        RoleId = ur.Role.Id,
                        RoleName = ur.Role.Name,
                        Description = ur.Role.Description
                    })
                    .Distinct()
                    .OrderBy(r => r.RoleName)
                    .ToList(),
                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user with context");
            return null;
        }
    }

    public async Task<bool> HasPermissionAsync(string permission, string? tenantId = null, int? projectId = null)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(tenantId, projectId);
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user permission: {Permission}", permission);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(string roleName, string? tenantId = null)
    {
        try
        {
            var roles = await GetUserRolesAsync(tenantId);
            return roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role: {RoleName}", roleName);
            return false;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string? tenantId = null, int? projectId = null)
    {
        try
        {
            var userWithContext = await GetCurrentUserWithContextAsync(tenantId, projectId);
            return userWithContext?.Permissions?.Select(p => p.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserRolesAsync(string? tenantId = null)
    {
        try
        {
            var userWithContext = await GetCurrentUserWithContextAsync(tenantId, null);
            return userWithContext?.Roles?.Select(r => r.RoleName).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles");
            return new List<string>();
        }
    }
}