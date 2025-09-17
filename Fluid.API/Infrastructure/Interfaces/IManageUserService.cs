using SharedKernel.Result;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IManageUserService
{
    /// <summary>
    /// Creates a new user with Azure AD integration and role assignment
    /// </summary>
    Task<Result<UserEM>> CreateUser(UserCM userCM);

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    Task<Result<UserEM>> GetUserById(int id);

    /// <summary>
    /// Checks if an email already exists in the system
    /// </summary>
    Task<Result<bool>> IsEmailExists(string email, int userId);

    /// <summary>
    /// Gets a list of all active users
    /// </summary>
    Task<IEnumerable<UserList>> ListUsersAsync();

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task<Result<UserCM>> UpdateUser(UserEM user);

    /// <summary>
    /// Updates user status (active/inactive) and syncs with Azure AD
    /// </summary>
    Task<Result<int>> UpdateUserStatusAsync(int userId, bool isActive);
}
