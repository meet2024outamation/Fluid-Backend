using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Interfaces;

public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    int GetCurrentUserId();
    
    /// <summary>
    /// Gets the current user's name from claims
    /// </summary>
    string GetCurrentUserName();
    
    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    string GetCurrentUserEmail();
    
    /// <summary>
    /// Gets the current user's full profile with basic information
    /// </summary>
    Task<CurrentUserProfile?> GetCurrentUserProfileAsync();
    
    /// <summary>
    /// Gets the current user's profile with context-scoped roles and permissions
    /// </summary>
    Task<UserMeResponse?> GetCurrentUserWithContextAsync(string? tenantId, int? projectId);
    
    /// <summary>
    /// Checks if the current user has a specific permission in the given context
    /// </summary>
    Task<bool> HasPermissionAsync(string permission, string? tenantId = null, int? projectId = null);
    
    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    Task<bool> HasRoleAsync(string roleName, string? tenantId = null);
    
    /// <summary>
    /// Gets all permissions for the current user in the given context
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(string? tenantId = null, int? projectId = null);
    
    /// <summary>
    /// Gets all roles for the current user in the given context
    /// </summary>
    Task<List<string>> GetUserRolesAsync(string? tenantId = null);
}

/// <summary>
/// Basic current user profile information
/// </summary>
public class CurrentUserProfile
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name => $"{FirstName} {LastName}".Trim();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}