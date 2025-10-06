using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;

namespace Fluid.API.Infrastructure.Services;

/// <summary>
/// Service for seeding permissions and role-permission assignments
/// </summary>
public interface IPermissionSeedingService
{
    Task SeedPermissionsAsync();
    Task SeedDefaultRolePermissionsAsync();
}

public class PermissionSeedingService : IPermissionSeedingService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<PermissionSeedingService> _logger;

    public PermissionSeedingService(FluidIAMDbContext iamContext, ILogger<PermissionSeedingService> logger)
    {
        _iamContext = iamContext;
        _logger = logger;
    }

    public async Task SeedPermissionsAsync()
    {
        _logger.LogInformation("Starting permission seeding");

        var permissions = GetAllPermissions();
        var existingPermissions = await _iamContext.Permissions
            .Select(p => p.Name)
            .ToHashSetAsync();

        var permissionsToAdd = new List<Permission>();

        foreach (var permission in permissions)
        {
            if (!existingPermissions.Contains(permission.Key))
            {
                permissionsToAdd.Add(new Permission
                {
                    Name = permission.Key,
                    Description = permission.Value,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                });
            }
        }

        if (permissionsToAdd.Any())
        {
            _iamContext.Permissions.AddRange(permissionsToAdd);
            await _iamContext.SaveChangesAsync();
            _logger.LogInformation("Added {Count} new permissions", permissionsToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new permissions to add");
        }
    }

    public async Task SeedDefaultRolePermissionsAsync()
    {
        _logger.LogInformation("Starting default role-permission assignments");

        var roles = await _iamContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        var permissions = await _iamContext.Permissions.ToListAsync();
        var permissionLookup = permissions.ToDictionary(p => p.Name, p => p.Id);

        var rolePermissionAssignments = GetDefaultRolePermissions();

        foreach (var assignment in rolePermissionAssignments)
        {
            var role = roles.FirstOrDefault(r => r.Name.Equals(assignment.Key, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                _logger.LogWarning("Role '{RoleName}' not found, skipping permission assignments", assignment.Key);
                continue;
            }

            var existingPermissions = role.RolePermissions
                .Select(rp => rp.Permission.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var permissionsToAdd = new List<RolePermission>();

            foreach (var permissionName in assignment.Value)
            {
                if (permissionLookup.TryGetValue(permissionName, out var permissionId) &&
                    !existingPermissions.Contains(permissionName))
                {
                    permissionsToAdd.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedDateTime = DateTimeOffset.UtcNow
                    });
                }
            }

            if (permissionsToAdd.Any())
            {
                _iamContext.RolePermissions.AddRange(permissionsToAdd);
                _logger.LogInformation("Adding {Count} permissions to role '{RoleName}'",
                    permissionsToAdd.Count, role.Name);
            }
        }

        await _iamContext.SaveChangesAsync();
        _logger.LogInformation("Completed default role-permission assignments");
    }

    private static Dictionary<string, string> GetAllPermissions()
    {
        return new Dictionary<string, string>
        {
            // Role management permissions
            { Authorization.ApplicationPermissions.CreateRoles, "Create roles" },
            { Authorization.ApplicationPermissions.ViewRoles, "View roles and related information" },
            { Authorization.ApplicationPermissions.UpdateRoles, "Update role information" },
            { Authorization.ApplicationPermissions.DeleteRoles, "Delete roles" },
            { Authorization.ApplicationPermissions.AssignRoles, "Assign roles to users" },

            // Permission management permissions
            { Authorization.ApplicationPermissions.CreatePermissions, "Create permissions" },
            { Authorization.ApplicationPermissions.ViewPermissions, "View permissions and related information" },
            { Authorization.ApplicationPermissions.UpdatePermissions, "Update permission information" },
            { Authorization.ApplicationPermissions.DeletePermissions, "Delete permissions" },

            // User management permissions
            { Authorization.ApplicationPermissions.CreateUsers, "Create users" },
            { Authorization.ApplicationPermissions.ViewUsers, "View users and related information" },
            { Authorization.ApplicationPermissions.UpdateUsers, "Update user information" },
            { Authorization.ApplicationPermissions.DeleteUsers, "Delete users" },

            // Tenant management permissions
            { Authorization.ApplicationPermissions.CreateTenants, "Create tenants" },
            { Authorization.ApplicationPermissions.ViewTenants, "View tenants and related information" },
            { Authorization.ApplicationPermissions.UpdateTenants, "Update tenant information" },
            { Authorization.ApplicationPermissions.DeleteTenants, "Delete tenants" },

            // Project management permissions
            { Authorization.ApplicationPermissions.CreateProjects, "Create projects" },
            { Authorization.ApplicationPermissions.ViewProjects, "View projects and related information" },
            { Authorization.ApplicationPermissions.UpdateProjects, "Update project information" },
            { Authorization.ApplicationPermissions.DeleteProjects, "Delete projects" },

            // Schema management permissions
            { Authorization.ApplicationPermissions.CreateSchemas, "Create schemas" },
            { Authorization.ApplicationPermissions.ViewSchemas, "View schemas and related information" },
            { Authorization.ApplicationPermissions.UpdateSchemas, "Update schema information" },
            { Authorization.ApplicationPermissions.DeleteSchemas, "Delete schemas" },

            // Order management permissions
            { Authorization.ApplicationPermissions.CreateOrders, "Create orders" },
            { Authorization.ApplicationPermissions.ViewOrders, "View orders and related information" },
            { Authorization.ApplicationPermissions.UpdateOrders, "Update order information" },
            { Authorization.ApplicationPermissions.DeleteOrders, "Delete orders" },
            { Authorization.ApplicationPermissions.AssignOrders, "Assign orders to users" },

            // Batch management permissions
            { Authorization.ApplicationPermissions.CreateBatches, "Create batches" },
            { Authorization.ApplicationPermissions.ViewBatches, "View batches and related information" },
            { Authorization.ApplicationPermissions.UpdateBatches, "Update batch information" },
            { Authorization.ApplicationPermissions.DeleteBatches, "Delete batches" },

            // Order Flow management permissions
            { Authorization.ApplicationPermissions.CreateOrderFlows, "Create order flows" },
            { Authorization.ApplicationPermissions.ViewOrderFlows, "View order flows and related information" },
            { Authorization.ApplicationPermissions.UpdateOrderFlows, "Update order flow information" },
            { Authorization.ApplicationPermissions.DeleteOrderFlows, "Delete order flows" },

            // Reporting permissions
            { Authorization.ApplicationPermissions.CreateReports, "Create reports" },
            { Authorization.ApplicationPermissions.ViewReports, "View reports and analytics" },
            { Authorization.ApplicationPermissions.UpdateReports, "Update report information" },
            { Authorization.ApplicationPermissions.DeleteReports, "Delete reports" },

            // System administration permissions
            { Authorization.ApplicationPermissions.SystemAdmin, "System administration access - full system control" },
            { Authorization.ApplicationPermissions.ViewAuditLogs, "View audit logs and system activity" }
        };
    }

    private static Dictionary<string, string[]> GetDefaultRolePermissions()
    {
        return new Dictionary<string, string[]>
        {
            [Authorization.ApplicationRoles.ProductOwner] = new[]
            {
                // Full system access
                Authorization.ApplicationPermissions.SystemAdmin,

                // Roles
                Authorization.ApplicationPermissions.CreateRoles,
                Authorization.ApplicationPermissions.ViewRoles,
                Authorization.ApplicationPermissions.UpdateRoles,
                Authorization.ApplicationPermissions.DeleteRoles,
                Authorization.ApplicationPermissions.AssignRoles,

                // Permissions
                Authorization.ApplicationPermissions.CreatePermissions,
                Authorization.ApplicationPermissions.ViewPermissions,
                Authorization.ApplicationPermissions.UpdatePermissions,
                Authorization.ApplicationPermissions.DeletePermissions,

                // Users
                Authorization.ApplicationPermissions.CreateUsers,
                Authorization.ApplicationPermissions.ViewUsers,
                Authorization.ApplicationPermissions.UpdateUsers,
                Authorization.ApplicationPermissions.DeleteUsers,

                // Tenants
                Authorization.ApplicationPermissions.CreateTenants,
                Authorization.ApplicationPermissions.ViewTenants,
                Authorization.ApplicationPermissions.UpdateTenants,
                Authorization.ApplicationPermissions.DeleteTenants,

                // Reports
                Authorization.ApplicationPermissions.CreateReports,
                Authorization.ApplicationPermissions.ViewReports,
                Authorization.ApplicationPermissions.UpdateReports,
                Authorization.ApplicationPermissions.DeleteReports,

                // Projects
                Authorization.ApplicationPermissions.CreateProjects,
                Authorization.ApplicationPermissions.ViewProjects,
                Authorization.ApplicationPermissions.UpdateProjects,
                Authorization.ApplicationPermissions.DeleteProjects,

                // Schemas
                Authorization.ApplicationPermissions.CreateSchemas,
                Authorization.ApplicationPermissions.ViewSchemas,
                Authorization.ApplicationPermissions.UpdateSchemas,
                Authorization.ApplicationPermissions.DeleteSchemas,

                // Orders
                Authorization.ApplicationPermissions.CreateOrders,
                Authorization.ApplicationPermissions.ViewOrders,
                Authorization.ApplicationPermissions.UpdateOrders,
                Authorization.ApplicationPermissions.DeleteOrders,
                Authorization.ApplicationPermissions.AssignOrders,

                // Batches
                Authorization.ApplicationPermissions.CreateBatches,
                Authorization.ApplicationPermissions.ViewBatches,
                Authorization.ApplicationPermissions.UpdateBatches,
                Authorization.ApplicationPermissions.DeleteBatches,
                // Audit logs
                Authorization.ApplicationPermissions.ViewAuditLogs
            },

            [Authorization.ApplicationRoles.TenantAdmin] = new[]
            {
                // Tenant-level administration

                // Roles
                Authorization.ApplicationPermissions.ViewRoles,
                //Authorization.ApplicationPermissions.AssignRoles,

                // Users
                //Authorization.ApplicationPermissions.CreateUsers,
                Authorization.ApplicationPermissions.ViewUsers,
                //Authorization.ApplicationPermissions.UpdateUsers,
                //Authorization.ApplicationPermissions.DeleteUsers,

                // Tenants
                Authorization.ApplicationPermissions.ViewTenants,
                //Authorization.ApplicationPermissions.UpdateTenants,

                // Projects
                Authorization.ApplicationPermissions.CreateProjects,
                Authorization.ApplicationPermissions.ViewProjects,
                Authorization.ApplicationPermissions.UpdateProjects,
                Authorization.ApplicationPermissions.DeleteProjects,

                // Schemas
                Authorization.ApplicationPermissions.CreateSchemas,
                Authorization.ApplicationPermissions.ViewSchemas,
                Authorization.ApplicationPermissions.UpdateSchemas,
                Authorization.ApplicationPermissions.DeleteSchemas,

                // Orders
                Authorization.ApplicationPermissions.CreateOrders,
                Authorization.ApplicationPermissions.ViewOrders,
                Authorization.ApplicationPermissions.UpdateOrders,
                Authorization.ApplicationPermissions.DeleteOrders,
                Authorization.ApplicationPermissions.AssignOrders,

                // Batches
                Authorization.ApplicationPermissions.CreateBatches,
                Authorization.ApplicationPermissions.ViewBatches,
                Authorization.ApplicationPermissions.UpdateBatches,
                Authorization.ApplicationPermissions.DeleteBatches,

                // OrderFlows
                Authorization.ApplicationPermissions.CreateOrderFlows,
                Authorization.ApplicationPermissions.ViewOrderFlows,
                Authorization.ApplicationPermissions.UpdateOrderFlows,
                Authorization.ApplicationPermissions.DeleteOrderFlows,
                // Reports
                Authorization.ApplicationPermissions.ViewReports
            },

            [Authorization.ApplicationRoles.Keying] = new[]
            {
                // Project and resource management (previously Manager)

                // Users (limited view only)
                Authorization.ApplicationPermissions.ViewUsers,

                // Projects
                Authorization.ApplicationPermissions.ViewProjects,
                //Authorization.ApplicationPermissions.UpdateProjects,

                // Schemas
                Authorization.ApplicationPermissions.ViewSchemas,

                // Orders
                Authorization.ApplicationPermissions.CreateOrders,
                Authorization.ApplicationPermissions.ViewOrders,
                Authorization.ApplicationPermissions.UpdateOrders,
                Authorization.ApplicationPermissions.DeleteOrders,
                Authorization.ApplicationPermissions.AssignOrders,

                // Batches
                Authorization.ApplicationPermissions.CreateBatches,
                Authorization.ApplicationPermissions.ViewBatches,
                Authorization.ApplicationPermissions.UpdateBatches,
                Authorization.ApplicationPermissions.DeleteBatches,

                // OrderFlows
                Authorization.ApplicationPermissions.CreateOrderFlows,
                Authorization.ApplicationPermissions.ViewOrderFlows,
                Authorization.ApplicationPermissions.UpdateOrderFlows,
                Authorization.ApplicationPermissions.DeleteOrderFlows,

                // Reports
                Authorization.ApplicationPermissions.ViewReports
            },

            [Authorization.ApplicationRoles.QC] = new[]
            {
                // Basic operational access (previously Operator)

                // Orders
                Authorization.ApplicationPermissions.ViewOrders,
                Authorization.ApplicationPermissions.UpdateOrders,

                // Batches
                Authorization.ApplicationPermissions.ViewBatches,

                // OrderFlows
                Authorization.ApplicationPermissions.ViewOrderFlows,
                Authorization.ApplicationPermissions.UpdateOrderFlows,

                // Projects
                Authorization.ApplicationPermissions.ViewProjects,

                // Schemas
                Authorization.ApplicationPermissions.ViewSchemas
            }
        };
    }
}