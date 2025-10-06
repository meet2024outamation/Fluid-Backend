using Microsoft.AspNetCore.Authorization;

namespace Fluid.API.Authorization;

/// <summary>
/// Authorization attribute that requires user to have specific permission(s)
/// </summary>
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public AuthorizePermissionAttribute(params string[] permissions)
    {
        Policy = string.Join(",", permissions);
    }
}