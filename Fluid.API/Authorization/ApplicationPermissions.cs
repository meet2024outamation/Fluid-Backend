namespace Fluid.API.Authorization;

/// <summary>
/// Constants for application permissions
/// </summary>
public static class ApplicationPermissions
{
    // Role management permissions
    public const string CreateRoles = "CreateRoles";
    public const string ViewRoles = "ViewRoles";
    public const string UpdateRoles = "UpdateRoles";
    public const string DeleteRoles = "DeleteRoles";
    public const string AssignRoles = "AssignRoles";

    // Permission management permissions
    public const string CreatePermissions = "CreatePermissions";
    public const string ViewPermissions = "ViewPermissions";
    public const string UpdatePermissions = "UpdatePermissions";
    public const string DeletePermissions = "DeletePermissions";

    // User management permissions
    public const string CreateUsers = "CreateUsers";
    public const string ViewUsers = "ViewUsers";
    public const string UpdateUsers = "UpdateUsers";
    public const string DeleteUsers = "DeleteUsers";

    // Tenant management permissions
    public const string CreateTenants = "CreateTenants";
    public const string ViewTenants = "ViewTenants";
    public const string UpdateTenants = "UpdateTenants";
    public const string DeleteTenants = "DeleteTenants";

    // Project management permissions
    public const string CreateProjects = "CreateProjects";
    public const string ViewProjects = "ViewProjects";
    public const string UpdateProjects = "UpdateProjects";
    public const string DeleteProjects = "DeleteProjects";

    // Schema management permissions
    public const string CreateSchemas = "CreateSchemas";
    public const string ViewSchemas = "ViewSchemas";
    public const string UpdateSchemas = "UpdateSchemas";
    public const string DeleteSchemas = "DeleteSchemas";

    // Order management permissions
    public const string CreateOrders = "CreateOrders";
    public const string ViewOrders = "ViewOrders";
    public const string UpdateOrders = "UpdateOrders";
    public const string DeleteOrders = "DeleteOrders";
    public const string AssignOrders = "AssignOrders";

    // Batch management permissions
    public const string CreateBatches = "CreateBatches";
    public const string ViewBatches = "ViewBatches";
    public const string UpdateBatches = "UpdateBatches";
    public const string DeleteBatches = "DeleteBatches";

    // Order Flow management permissions
    public const string CreateOrderFlows = "CreateOrderFlows";
    public const string ViewOrderFlows = "ViewOrderFlows";
    public const string UpdateOrderFlows = "UpdateOrderFlows";
    public const string DeleteOrderFlows = "DeleteOrderFlows";

    // Reporting permissions
    public const string CreateReports = "CreateReports";
    public const string ViewReports = "ViewReports";
    public const string UpdateReports = "UpdateReports";
    public const string DeleteReports = "DeleteReports";

    // System administration permissions
    public const string SystemAdmin = "SystemAdmin";
    public const string ViewAuditLogs = "ViewAuditLogs";
}