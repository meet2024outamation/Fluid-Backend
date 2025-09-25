using Microsoft.AspNetCore.Authorization;

namespace Fluid.API.Authorization
{
    public class RequireTenantAccessAttribute : AuthorizeAttribute
    {
        public RequireTenantAccessAttribute() : base("RequireTenantAccess") { }
    }
}
