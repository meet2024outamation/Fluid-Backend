using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Models;
using SharedKernel.Result;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Services;

public class ManageUserService : IManageUserService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly FluidDbContext _context;
    private readonly IGraphService _graphService;
    private readonly AzureADConfig _azureADConfig;
    private readonly ILogger<ManageUserService> _logger;

    public ManageUserService(
        FluidIAMDbContext iamContext,
        FluidDbContext context,
        IGraphService graphService,
        IOptions<AzureADConfig> azureAdConfig,
        ILogger<ManageUserService> logger)
    {
        _graphService = graphService;
        _iamContext = iamContext;
        _context = context;
        _azureADConfig = azureAdConfig.Value;
        _logger = logger;
    }

    #region New Simplified Methods

    public async Task<Result<UserResponse>> CreateUserAsync(UserRequest request)
    {
        try
        {
            // Check if email already exists
            var emailCheck = await IsEmailExistsAsync(request.Email);
            if (!emailCheck.IsSuccess)
            {
                return Result<UserResponse>.Invalid(emailCheck.ValidationErrors);
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                var user = new Entities.IAM.User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle Azure AD integration
                var azureAdUser = await _graphService.GetUserByEmail(user.Email);

                if (azureAdUser == null)
                {
                    var invitation = await _graphService.InviteGuestUser(user);

                    if (invitation?.InvitedUser?.Id == null)
                    {
                        return Result<UserResponse>.Invalid(new List<ValidationError>
                        {
                            new ValidationError { Key = "User", ErrorMessage = $"{request.Email} could not be added to Azure AD." }
                        });
                    }

                    user.AzureAdId = invitation.InvitedUser.Id;
                }
                else
                {
                    user.AzureAdId = azureAdUser.Id;
                }

                if (!string.IsNullOrWhiteSpace(user.AzureAdId))
                {
                    await _graphService.UserAssignment(user.AzureAdId);
                }

                await _iamContext.Users.AddAsync(user);
                await _iamContext.SaveChangesAsync();

                // Assign roles if provided
                if (request.Roles?.Count > 0)
                {
                    var userRoles = request.Roles.Select(projectRole => new UserRole
                    {
                        RoleId = projectRole.RoleId,
                        UserId = user.Id,
                        TenantId = projectRole.TenantId,
                        ProjectId = projectRole.ProjectId,
                        CreatedDateTime = DateTimeOffset.UtcNow
                    }).ToList();

                    await _iamContext.UserRoles.AddRangeAsync(userRoles);
                    await _iamContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Return the created user
                var result = await GetUserByIdAsync(user.Id);
                return result.IsSuccess
                    ? Result<UserResponse>.Created(result.Value!, "User created successfully")
                    : Result<UserResponse>.Error("User created but could not retrieve details");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Result<UserResponse>.Error("User creation failed.");
        }
    }

    public async Task<Result<UserResponse>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Tenant)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return Result<UserResponse>.NotFound();

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = (await Task.WhenAll(user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive)
                    .Select(async ur => new ProjectRoleResponse
                    {
                        TenantId = ur.TenantId,
                        ProjectId = ur.ProjectId ?? 0, // Handle null ProjectId for Product Owner and Tenant Owner
                        RoleId = ur.Role.Id,
                        TenantName = ur.Tenant?.Name,
                        ProjectName = ur.ProjectId.HasValue ? await GetProjectNameAsync(ur.ProjectId.Value) : null,
                        RoleName = ur.Role.Name
                    }))).ToList()
            };

            return Result<UserResponse>.Success(userResponse, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return Result<UserResponse>.Error("An error occurred while retrieving the user.");
        }
    }

    public async Task<Result<UserMeResponse>> GetCurrentUserAsync(int id)
    {
        try
        {
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return Result<UserMeResponse>.NotFound();

            var userMeResponse = new UserMeResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoleUsers
                    .Where(ur => ur.Role.IsActive)
                    .Select(ur => new UserRoleInfo
                    {
                        RoleId = ur.Role.Id,
                        RoleName = ur.Role.Name,
                        Description = ur.Role.Description
                    }).ToList()
            };

            return Result<UserMeResponse>.Success(userMeResponse, "Current user retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user with ID {UserId}", id);
            return Result<UserMeResponse>.Error("An error occurred while retrieving the current user.");
        }
    }

    // Helper method to get project name from the database
    private async Task<string?> GetProjectNameAsync(int projectId)
    {
        try
        {
            // Return null for invalid project IDs (used for Product Owner and Tenant Owner roles)
            if (projectId <= 0)
            {
                return null;
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            return project?.Name ?? $"Project {projectId}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve project name for ProjectId: {ProjectId}", projectId);
            return projectId > 0 ? $"Project {projectId}" : null;
        }
    }

    public async Task<Result<List<UserListItem>>> GetUsersAsync(bool? isActive = null)
    {
        try
        {
            var query = _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query.OrderBy(u => u.FirstName).ToListAsync();

            var userList = users.Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                RoleNames = u.UserRoleUsers
                    .Where(ur => ur.Role.IsActive)
                    .Select(ur => ur.Role.Name)
                    .ToList()
            }).ToList();

            return Result<List<UserListItem>>.Success(userList, $"Retrieved {userList.Count} users successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return Result<List<UserListItem>>.Error("An error occurred while retrieving users.");
        }
    }

    public async Task<Result<UserResponse>> UpdateUserAsync(int id, UserRequest request)
    {
        try
        {
            var existingUser = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                return Result<UserResponse>.NotFound();

            // Check email uniqueness (excluding current user)
            var emailCheck = await IsEmailExistsAsync(request.Email, id);
            if (!emailCheck.IsSuccess)
            {
                return Result<UserResponse>.Invalid(emailCheck.ValidationErrors);
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Update user properties
                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.Email = request.Email;
                existingUser.Phone = request.Phone;
                existingUser.IsActive = request.IsActive;
                existingUser.UpdatedAt = DateTime.UtcNow;

                // Update roles if provided
                if (request.Roles?.Count > 0)
                {
                    // Remove existing roles
                    var existingRoles = existingUser.UserRoleUsers.ToList();
                    _iamContext.UserRoles.RemoveRange(existingRoles);

                    // Add new roles
                    var userRoles = request.Roles.Select(projectRole => new UserRole
                    {
                        RoleId = projectRole.RoleId,
                        UserId = existingUser.Id,
                        TenantId = projectRole.TenantId,
                        ProjectId = projectRole.ProjectId,
                        CreatedDateTime = DateTimeOffset.UtcNow
                    }).ToList();

                    await _iamContext.UserRoles.AddRangeAsync(userRoles);
                }

                await _iamContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated user
                var result = await GetUserByIdAsync(id);
                return result.IsSuccess
                    ? Result<UserResponse>.Success(result.Value!, "User updated successfully")
                    : Result<UserResponse>.Error("User updated but could not retrieve details");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", id);
            return Result<UserResponse>.Error("An error occurred while updating the user.");
        }
    }

    public async Task<Result<bool>> UpdateUserStatusAsync(int id, bool isActive)
    {
        try
        {
            var existingUser = await _iamContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
                return Result<bool>.NotFound();

            if (!string.IsNullOrWhiteSpace(existingUser.AzureAdId))
            {
                await _graphService.DisabledUser(existingUser.AzureAdId, isActive);
            }

            existingUser.IsActive = isActive;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();
            return Result<bool>.Success(true, "User status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for ID {UserId}", id);
            return Result<bool>.Error("An error occurred while updating user status.");
        }
    }

    public async Task<Result<bool>> DeleteUserAsync(int id)
    {
        try
        {
            var existingUser = await _iamContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
                return Result<bool>.NotFound();

            // Soft delete - just mark as inactive
            existingUser.IsActive = false;
            existingUser.UpdatedAt = DateTime.UtcNow;

            // Also disable in Azure AD
            if (!string.IsNullOrWhiteSpace(existingUser.AzureAdId))
            {
                await _graphService.DisabledUser(existingUser.AzureAdId, false);
            }

            await _iamContext.SaveChangesAsync();
            return Result<bool>.Success(true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
            return Result<bool>.Error("An error occurred while deleting the user.");
        }
    }

    public async Task<Result<bool>> IsEmailExistsAsync(string email, int? excludeUserId = null)
    {
        try
        {
            var query = _iamContext.Users.Where(u => u.Email.ToLower() == email.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var existingUser = await query.FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return Result<bool>.Invalid(new List<ValidationError>
                {
                    new ValidationError { Key = "Email", ErrorMessage = $"Email '{email}' is already in use." }
                });
            }

            return Result<bool>.Success(false, "Email is available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence for {Email}", email);
            return Result<bool>.Error("An error occurred while checking email availability.");
        }
    }

    #endregion

    #region Legacy Methods (for backward compatibility)

    [Obsolete("Use CreateUserAsync instead")]
    public async Task<Result<UserEM>> CreateUser(UserCM userCM)
    {
        try
        {
            var request = new UserRequest
            {
                Email = userCM.Email,
                FirstName = userCM.FirstName,
                LastName = userCM.LastName,
                Phone = userCM.PhoneNumber,
                IsActive = userCM.IsActive,
                // Convert simple RoleIds to ProjectRoles - this is a simplified conversion
                //Roles = userCM.RoleIds.Select(roleId => new ProjectRoleResponse
                //{
                //    RoleId = roleId,
                //    TenantId = "default", // Default tenant for legacy compatibility
                //    ProjectId = 1 // Default project for legacy compatibility
                //}).ToList()
            };

            var result = await CreateUserAsync(request);
            if (!result.IsSuccess)
                return Result<UserEM>.Error(result.Errors.ToArray());

            return Result<UserEM>.Created(new UserEM
            {
                Id = result.Value!.Id,
                FirstName = result.Value.FirstName,
                LastName = result.Value.LastName,
                Email = result.Value.Email
            }, result.SuccessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in legacy CreateUser method");
            return Result<UserEM>.Error("User creation failed.");
        }
    }

    [Obsolete("Use GetUserByIdAsync instead")]
    public async Task<Result<UserEM>> GetUserById(int id)
    {
        var result = await GetUserByIdAsync(id);
        if (!result.IsSuccess)
            return Result<UserEM>.Error(result.Errors.ToArray());

        return Result<UserEM>.Success(new UserEM
        {
            Id = result.Value!.Id,
            FirstName = result.Value.FirstName,
            LastName = result.Value.LastName,
            Email = result.Value.Email
        }, result.SuccessMessage);
    }

    [Obsolete("Use GetUsersAsync instead")]
    public async Task<IEnumerable<UserList>> ListUsersAsync()
    {
        try
        {
            var result = await GetUsersAsync(true); // Only active users
            if (!result.IsSuccess)
                return new List<UserList>();

            return result.Value!.Select(u => new UserList
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedOn = u.CreatedAt,
                ModifiedOn = u.CreatedAt, // Legacy doesn't have UpdatedAt
                Team = string.Empty,
                TeamId = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in legacy ListUsersAsync method");
            return new List<UserList>();
        }
    }

    [Obsolete("Use UpdateUserAsync instead")]
    public async Task<Result<UserCM>> UpdateUser(UserEM userEM)
    {
        try
        {
            if (!userEM.Id.HasValue)
                return Result<UserCM>.Invalid(new List<ValidationError>
                {
                    new ValidationError { Key = "Id", ErrorMessage = "User ID is required." }
                });

            var request = new UserRequest
            {
                Email = userEM.Email,
                FirstName = userEM.FirstName,
                LastName = userEM.LastName,
                IsActive = userEM.IsActive,
                // Convert simple RoleIds to ProjectRoles - this is a simplified conversion
                //Roles = userEM.RoleIds.Select(roleId => new ProjectRoleResponse
                //{
                //    RoleId = roleId,
                //    TenantId = "default", // Default tenant for legacy compatibility
                //    ProjectId = 1 // Default project for legacy compatibility
                //}).ToList()
            };

            var result = await UpdateUserAsync(userEM.Id.Value, request);
            if (!result.IsSuccess)
                return Result<UserCM>.Error(result.Errors.ToArray());

            return Result<UserCM>.Success(new UserCM
            {
                FirstName = result.Value!.FirstName,
                LastName = result.Value.LastName,
                Email = result.Value.Email
            }, result.SuccessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in legacy UpdateUser method");
            return Result<UserCM>.Error("An error occurred while updating the user.");
        }
    }

    [Obsolete("Use UpdateUserStatusAsync instead")]
    async Task<Result<int>> IManageUserService.UpdateUserStatusAsyncLegacy(int userId, bool isActive)
    {
        var result = await UpdateUserStatusAsync(userId, isActive);
        return result.IsSuccess
            ? Result<int>.Success(userId, result.SuccessMessage)
            : Result<int>.Error(result.Errors.ToArray());
    }

    [Obsolete("Use IsEmailExistsAsync instead")]
    public async Task<Result<bool>> IsEmailExists(string email, int userId)
    {
        return await IsEmailExistsAsync(email, userId);
    }

    #endregion

    public async Task<Result<Fluid.API.Models.User.AccessibleTenantsResponse>> GetAccessibleTenantsByIdentifierAsync(string userIdentifier)
    {
        try
        {
            // Get user with their roles across tenants and projects using identifier (email or username)
            var user = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Tenant)
                .Where(u => (u.Email == userIdentifier || u.AzureAdId.Contains(userIdentifier)) && u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found with identifier: {UserIdentifier}", userIdentifier);
                return Result<Fluid.API.Models.User.AccessibleTenantsResponse>.NotFound();
            }

            // Check for Product Owner role (global role with null tenant and project)
            var isProductOwner = user.UserRoleUsers
                .Any(ur => ur.Role.IsActive &&
                          ur.Role.Name == ApplicationRoles.ProductOwner &&
                          ur.TenantId == null &&
                          ur.ProjectId == null);

            _logger.LogDebug("User {UserIdentifier} Product Owner status: {IsProductOwner}", userIdentifier, isProductOwner);

            // Get Tenant Owner roles (roles with null project but specific tenant)
            var tenantOwnerRoles = user.UserRoleUsers
                .Where(ur => ur.Role.IsActive &&
                            ur.Role.Name == ApplicationRoles.TenantAdmin &&
                            ur.ProjectId == null &&
                            ur.TenantId != null &&
                            ur.Tenant != null &&
                            ur.Tenant.IsActive)
                .ToList();

            var tenantOwnerTenantIds = tenantOwnerRoles
                .Select(ur => ur.TenantId!)
                .Distinct()
                .ToList();

            _logger.LogDebug("User {UserIdentifier} has Tenant Owner access to {TenantCount} tenants", userIdentifier, tenantOwnerTenantIds.Count);

            // Group user roles by tenant for regular project-based access
            // Only include roles that have both tenant and project (exclude Product Owner and Tenant Owner)
            var tenantRoles = user.UserRoleUsers
                .Where(ur => ur.Role.IsActive &&
                            ur.Tenant != null &&
                            ur.Tenant.IsActive &&
                            ur.ProjectId != null &&
                            ur.ProjectId > 0 && // Ensure valid project ID
                            ur.Role.Name != ApplicationRoles.ProductOwner && // Exclude Product Owner roles
                            ur.Role.Name != ApplicationRoles.TenantAdmin) // Exclude Tenant Owner roles
                .GroupBy(ur => ur.Tenant!)
                .ToList();

            var accessibleTenants = new List<Fluid.API.Models.User.AccessibleTenant>();

            foreach (var tenantGroup in tenantRoles)
            {
                var tenant = tenantGroup.Key;
                var rolesInTenant = tenantGroup.ToList();

                // Get projects accessible to this user in this tenant
                var projectIds = rolesInTenant
                    .Where(ur => ur.ProjectId.HasValue && ur.ProjectId.Value > 0)
                    .Select(ur => ur.ProjectId!.Value)
                    .Distinct()
                    .ToList();

                var accessibleProjects = new List<Fluid.API.Models.User.AccessibleProject>();

                if (projectIds.Any())
                {
                    // Switch to tenant-specific database context to get projects
                    try
                    {
                        var tenantDbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                            .UseNpgsql(tenant.ConnectionString)
                            .Options;

                        using var tenantContext = new FluidDbContext(tenantDbOptions, tenant);

                        var projects = await tenantContext.Projects
                            .Where(p => projectIds.Contains(p.Id) && p.IsActive)
                            .ToListAsync();

                        accessibleProjects = projects.Select(project =>
                        {
                            // Get user roles for this specific project
                            var projectRoles = rolesInTenant
                                .Where(ur => ur.ProjectId == project.Id)
                                .Select(ur => ur.Role.Name)
                                .Distinct()
                                .ToList();

                            return new Fluid.API.Models.User.AccessibleProject
                            {
                                ProjectId = project.Id,
                                ProjectName = project.Name,
                                ProjectCode = project.Code,
                                Description = null, // Project entity doesn't have Description
                                IsActive = project.IsActive,
                                UserRoles = projectRoles,
                                CreatedAt = project.CreatedAt
                            };
                        }).ToList();

                        _logger.LogDebug("Retrieved {ProjectCount} projects for tenant {TenantName}", projects.Count, tenant.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not access tenant database for tenant {TenantId}. Tenant: {TenantName}",
                            tenant.Id, tenant.Name);
                        // Continue with empty projects list for this tenant
                    }
                }

                var accessibleTenant = new Fluid.API.Models.User.AccessibleTenant
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    TenantIdentifier = tenant.Identifier,
                    Description = tenant.Description,
                    UserRoles = rolesInTenant.Select(ur => ur.Role.Name).Distinct().ToList(),
                    Projects = accessibleProjects
                };

                accessibleTenants.Add(accessibleTenant);
            }

            var response = new Fluid.API.Models.User.AccessibleTenantsResponse
            {
                UserId = user.Id,
                UserName = user.Name,
                Email = user.Email,
                IsProductOwner = isProductOwner,
                TenantAdminTenantIds = tenantOwnerTenantIds,
                Tenants = accessibleTenants.OrderBy(t => t.TenantName).ToList()
            };

            _logger.LogInformation("Retrieved {TenantCount} accessible tenants with {ProjectCount} total projects for user {UserIdentifier}. IsProductOwner: {IsProductOwner}, TenantOwnerCount: {TenantOwnerCount}",
                response.Tenants.Count,
                response.Tenants.Sum(t => t.ProjectCount),
                userIdentifier,
                isProductOwner,
                tenantOwnerTenantIds.Count);

            return Result<Fluid.API.Models.User.AccessibleTenantsResponse>.Success(response,
                $"Retrieved {response.Tenants.Count} accessible tenants with {response.Tenants.Sum(t => t.ProjectCount)} total projects");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accessible tenants for user identifier {UserIdentifier}", userIdentifier);
            return Result<Fluid.API.Models.User.AccessibleTenantsResponse>.Error("An error occurred while retrieving accessible tenants and projects.");
        }
    }
}
