using SharedKernel.Result;
using Fluid.API.Models.User;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IManageUserService
{
    /// <summary>
    /// Creates a new user with Azure AD integration and role assignment
    /// </summary>
    Task<Result<UserResponse>> CreateUserAsync(UserRequest request);

    /// <summary>
    /// Gets a user by their ID with role information (includes ProjectRole details)
    /// </summary>
    Task<Result<UserResponse>> GetUserByIdAsync(int id);

    /// <summary>
    /// Gets current user information with context-scoped role details (for "Me" endpoint)
    /// Handles different scoping rules based on user's primary role type
    /// </summary>
    Task<Result<UserMeResponse>> GetCurrentUserAsync(int id, string? tenantId, int? projectId);

    /// <summary>
    /// Gets accessible tenants and projects for the current user by user identifier
    /// </summary>
    Task<Result<Fluid.API.Models.User.AccessibleTenantsResponse>> GetAccessibleTenantsByIdentifierAsync(string userIdentifier);

    /// <summary>
    /// Gets a list of all users (with optional filtering by active status)
    /// </summary>
    Task<Result<List<UserListItem>>> GetUsersAsync(bool? isActive = null);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task<Result<UserResponse>> UpdateUserAsync(int id, UserRequest request);

    /// <summary>
    /// Updates user status (active/inactive) and syncs with Azure AD
    /// </summary>
    Task<Result<bool>> UpdateUserStatusAsync(int id, bool isActive);

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    Task<Result<bool>> DeleteUserAsync(int id);

    /// <summary>
    /// Checks if an email already exists in the system
    /// </summary>
    Task<Result<bool>> IsEmailExistsAsync(string email, int? excludeUserId = null);

    #region Legacy Methods (for backward compatibility)
    
    [Obsolete("Use CreateUserAsync instead")]
    Task<Result<UserEM>> CreateUser(UserCM userCM);

    [Obsolete("Use GetUserByIdAsync instead")]
    Task<Result<UserEM>> GetUserById(int id);

    [Obsolete("Use GetUsersAsync instead")]
    Task<IEnumerable<UserList>> ListUsersAsync();

    [Obsolete("Use UpdateUserAsync instead")]
    Task<Result<UserCM>> UpdateUser(UserEM user);

    [Obsolete("Use UpdateUserStatusAsync instead")]
    Task<Result<int>> UpdateUserStatusAsyncLegacy(int userId, bool isActive);

    [Obsolete("Use IsEmailExistsAsync instead")]
    Task<Result<bool>> IsEmailExists(string email, int userId);

    #endregion
}
