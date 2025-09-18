using Microsoft.AspNetCore.Authorization;

namespace Fluid.API.Authorization;

/// <summary>
/// Authorization attribute that requires user to have specific role(s)
/// </summary>
public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}