namespace Fluid.API.Authorization;

/// <summary>
/// Constants for application roles
/// </summary>
public static class ApplicationRoles
{
    public const string ProductOwner = "Product Owner";
    public const string TenantAdmin = "Tenant Admin";
    public const string Manager = "Manager";
    public const string Operator = "Operator";
}

/// <summary>
/// Constants for authorization policies
/// </summary>
public static class AuthorizationPolicies
{
    public const string ProductOwnerPolicy = "ProductOwnerPolicy";
    public const string TenantAdminPolicy = "TenantAdminPolicy";
    public const string ManagerPolicy = "ManagerPolicy";
    public const string OperatorPolicy = "OperatorPolicy";
}