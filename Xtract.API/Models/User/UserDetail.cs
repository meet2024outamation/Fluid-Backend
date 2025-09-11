using Microsoft.AspNetCore.Mvc;
using SharedKernel.Services;

namespace Xtract.API.Models.User;

public class UserDetail : UserBasicInfo
{

    public IList<PermissionDTO> UserPermissions { get; set; } = null!;
    public int RoleId => Roles.First().RoleId;
    public string RoleName => Roles.First().RoleName;

    public UserDetail()
    {
        UserPermissions = new List<PermissionDTO>();
    }
}


public class PermissionDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsRolePermission { get; set; }
}

public class UserRoleDTO
{
    public int RoleId { get; set; }
    public int UserId { get; set; }
    public string RoleName { get; set; } = null!;
    public IList<PermissionDTO> Permissions { get; set; } = null!;
    public UserRoleDTO()
    {
        Permissions = new List<PermissionDTO>();
    }
}

public class UserPermissionDTO
{
    public int PermissionId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public IList<PermissionDTO> Permissions { get; set; } = null!;
    public UserPermissionDTO()
    {
        Permissions = new List<PermissionDTO>();
    }
}

public class ModuleDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class RoleDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int StatusId { get; internal set; }
    public bool IsEditable { get; internal set; }
    public List<int> PermissionIds { get; set; } = [];
}
public class RoleDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string TenantName { get; set; } = null!;
    public bool IsEditable { get; set; }
    public int StatusId { get; set; }
    public bool IsActive { get; set; }
    public string RoleStatus { get; set; } = null!;
    public int NumberOfUsers { get; set; }
    public List<PermissionDTO> Permissions { get; set; } = [];
}
public class RolePermissionDTO
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public int? ModifiedById { get; set; }

    public PermissionDTO Permission { get; set; } = null!;
    public RoleDTO Role { get; set; } = null!;
}

public class UpdatePermissionStatusRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public bool IsActive { get; set; }
}

public class UpdateRoleStatusRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public bool IsActive { get; set; }
}
