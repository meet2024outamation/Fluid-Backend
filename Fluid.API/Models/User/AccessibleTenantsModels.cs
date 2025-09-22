namespace Fluid.API.Models.User;

/// <summary>
/// Response model for accessible tenants and projects
/// </summary>
public class AccessibleTenantsResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// True if user has Product Owner role with global access (null tenant and project)
    /// When true, user gets immediate access to User Management + Tenant Management without tenant selection
    /// </summary>
    public bool IsProductOwner { get; set; }

    /// <summary>
    /// List of tenants where user has Tenant Owner/Admin role with detailed tenant information
    /// These tenants appear in tenant selection screen for tenant-level management
    /// </summary>
    public List<TenantAdminInfo> TenantAdminIds { get; set; } = new List<TenantAdminInfo>();

    /// <summary>
    /// Regular tenant access with project-specific roles
    /// </summary>
    public List<AccessibleTenant> Tenants { get; set; } = new List<AccessibleTenant>();
}

/// <summary>
/// Tenant information for admin/owner access
/// </summary>
public class TenantAdminInfo
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantIdentifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Accessible tenant with projects
/// </summary>
public class AccessibleTenant
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantIdentifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> UserRoles { get; set; } = new List<string>();
    public List<AccessibleProject> Projects { get; set; } = new List<AccessibleProject>();
    public int ProjectCount => Projects.Count;
}

/// <summary>
/// Accessible project within a tenant
/// </summary>
public class AccessibleProject
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<string> UserRoles { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}