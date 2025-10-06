using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Role;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset? ModifiedDateTime { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new List<int>();
}

public class UpdateRoleRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new List<int>();
}

public class RoleListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int PermissionCount { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}