using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Project;

public class CreateProjectRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; }
}

public class UpdateProjectRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; }
}

public class UpdateProjectStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

public class ProjectResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatedByName { get; set; } = string.Empty;
}

public class ProjectListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response model for tenant-based project listing
/// </summary>
public class TenantProjectsResponse
{
    public List<TenantWithProjects> Tenants { get; set; } = new List<TenantWithProjects>();
    public int TotalTenants { get; set; }
    public int TotalProjects { get; set; }
}

/// <summary>
/// Tenant information with its associated projects
/// </summary>
public class TenantWithProjects
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantIdentifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<ProjectInTenant> Projects { get; set; } = new List<ProjectInTenant>();
    public int ProjectCount => Projects.Count;
}

/// <summary>
/// Project information within a tenant
/// </summary>
public class ProjectInTenant
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
}