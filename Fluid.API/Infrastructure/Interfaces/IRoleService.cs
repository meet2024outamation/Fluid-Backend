using Fluid.API.Models.Role;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IRoleService
{
    Task<Result<List<RoleListDto>>> GetAllRolesAsync();
    Task<Result<RoleDto>> GetRoleByIdAsync(int id);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request);
    Task<Result<bool>> DeleteRoleAsync(int id);
    Task<Result<List<PermissionDto>>> GetAllPermissionsAsync();
}