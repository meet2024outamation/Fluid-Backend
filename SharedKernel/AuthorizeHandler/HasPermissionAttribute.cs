using Microsoft.AspNetCore.Authorization;

namespace SharedKernel.AuthorizeHandler
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permissions) : base(policy: permissions)
        {
        }
    }
}
