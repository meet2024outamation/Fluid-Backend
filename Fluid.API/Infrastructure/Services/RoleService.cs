using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Role;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using SharedKernel.Services;

namespace Fluid.API.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RoleService> _logger;

    public RoleService(FluidIAMDbContext iamContext, ICurrentUserService currentUserService, ILogger<RoleService> logger)
    {
        _iamContext = iamContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<RoleListDto>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _iamContext.Roles
                .Include(r => r.RolePermissions)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new RoleListDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    PermissionCount = r.RolePermissions.Count(rp => rp.Permission.IsActive),
                    CreatedDateTime = r.CreatedDateTime
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} roles", roles.Count);
            return Result<List<RoleListDto>>.Success(roles, $"Retrieved {roles.Count} roles successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Result<List<RoleListDto>>.Error("An error occurred while retrieving roles.");
        }
    }

    public async Task<Result<RoleDto>> GetRoleByIdAsync(int id)
    {
        try
        {
            var role = await _iamContext.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (role == null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", id);
                return Result<RoleDto>.NotFound();
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedDateTime = role.CreatedDateTime,
                ModifiedDateTime = role.ModifiedDateTime,
                Permissions = role.RolePermissions
                    .Where(rp => rp.Permission.IsActive)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        IsActive = rp.Permission.IsActive
                    })
                    .OrderBy(p => p.Name)
                    .ToList()
            };

            _logger.LogInformation("Retrieved role with ID: {RoleId}", id);
            return Result<RoleDto>.Success(roleDto, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            return Result<RoleDto>.Error("An error occurred while retrieving the role.");
        }
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();

            // Check if role name already exists
            var existingRole = await _iamContext.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower());

            if (existingRole != null)
            {
                _logger.LogWarning("Attempted to create role with existing name: {RoleName}", request.Name);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = "A role with this name already exists."
                };

                return Result<RoleDto>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate permission IDs exist
            var validPermissionIds = new HashSet<int>();
            if (request.PermissionIds.Any())
            {
                validPermissionIds = await _iamContext.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id) && p.IsActive)
                    .Select(p => p.Id)
                    .ToHashSetAsync();

                var invalidPermissionIds = request.PermissionIds.Except(validPermissionIds).ToList();
                if (invalidPermissionIds.Any())
                {
                    var validationError = new ValidationError
                    {
                        Key = nameof(request.PermissionIds),
                        ErrorMessage = $"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}"
                    };

                    return Result<RoleDto>.Invalid(new List<ValidationError> { validationError });
                }
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Create role
                var role = new Role
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedById = currentUserId
                };

                _iamContext.Roles.Add(role);
                await _iamContext.SaveChangesAsync();

                // Add role permissions
                if (validPermissionIds.Any())
                {
                    var rolePermissions = validPermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedDateTime = DateTimeOffset.UtcNow,
                        CreatedById = currentUserId
                    }).ToList();

                    _iamContext.RolePermissions.AddRange(rolePermissions);
                    await _iamContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Return created role
                var result = await GetRoleByIdAsync(role.Id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Role '{RoleName}' created successfully with ID {RoleId}", role.Name, role.Id);
                    return Result<RoleDto>.Created(result.Value!, "Role created successfully");
                }

                return Result<RoleDto>.Error("Role created but failed to retrieve details");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with name: {RoleName}", request.Name);
            return Result<RoleDto>.Error("An error occurred while creating the role.");
        }
    }

    public async Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();

            var role = await _iamContext.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (role == null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found for update", id);
                return Result<RoleDto>.NotFound();
            }

            // Check if role name already exists (excluding current role)
            var existingRole = await _iamContext.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower() && r.Id != id && r.IsActive);

            if (existingRole != null)
            {
                _logger.LogWarning("Attempted to update role {RoleId} with existing name: {RoleName}", id, request.Name);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = "A role with this name already exists."
                };

                return Result<RoleDto>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate permission IDs exist
            var validPermissionIds = new HashSet<int>();
            if (request.PermissionIds.Any())
            {
                validPermissionIds = await _iamContext.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id) && p.IsActive)
                    .Select(p => p.Id)
                    .ToHashSetAsync();

                var invalidPermissionIds = request.PermissionIds.Except(validPermissionIds).ToList();
                if (invalidPermissionIds.Any())
                {
                    var validationError = new ValidationError
                    {
                        Key = nameof(request.PermissionIds),
                        ErrorMessage = $"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}"
                    };

                    return Result<RoleDto>.Invalid(new List<ValidationError> { validationError });
                }
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Update role
                role.Name = request.Name.Trim();
                role.Description = request.Description?.Trim();
                role.ModifiedDateTime = DateTimeOffset.UtcNow;
                role.ModifiedById = currentUserId;

                // Remove existing role permissions
                _iamContext.RolePermissions.RemoveRange(role.RolePermissions);

                // Add new role permissions
                if (validPermissionIds.Any())
                {
                    var newRolePermissions = validPermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedDateTime = DateTimeOffset.UtcNow,
                        CreatedById = currentUserId
                    }).ToList();

                    _iamContext.RolePermissions.AddRange(newRolePermissions);
                }

                await _iamContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated role
                var result = await GetRoleByIdAsync(role.Id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Role '{RoleName}' updated successfully", role.Name);
                    return Result<RoleDto>.Success(result.Value!, "Role updated successfully");
                }

                return Result<RoleDto>.Error("Role updated but failed to retrieve details");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return Result<RoleDto>.Error("An error occurred while updating the role.");
        }
    }

    public async Task<Result<bool>> DeleteRoleAsync(int id)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();

            var role = await _iamContext.Roles
                .Include(r => r.UserRoleUsers)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (role == null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Check if role is assigned to any users
            if (role.UserRoleUsers.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Role",
                    ErrorMessage = $"Cannot delete role '{role.Name}' as it is assigned to {role.UserRoleUsers.Count} user(s). Please remove the role from all users first."
                };

                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Soft delete - mark as inactive
                role.IsActive = false;
                role.ModifiedDateTime = DateTimeOffset.UtcNow;
                role.ModifiedById = currentUserId;

                // Remove role permissions
                var rolePermissions = await _iamContext.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _iamContext.RolePermissions.RemoveRange(rolePermissions);

                await _iamContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Role '{RoleName}' deleted successfully", role.Name);
                return Result<bool>.Success(true, "Role deleted successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", id);
            return Result<bool>.Error("An error occurred while deleting the role.");
        }
    }

    public async Task<Result<List<PermissionDto>>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _iamContext.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} permissions", permissions.Count);
            return Result<List<PermissionDto>>.Success(permissions, $"Retrieved {permissions.Count} permissions successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return Result<List<PermissionDto>>.Error("An error occurred while retrieving permissions.");
        }
    }
}