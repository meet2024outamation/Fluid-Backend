using Microsoft.AspNetCore.Authorization;

namespace SharedKernel.AuthorizeHandler
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string permission)
        {
            Permission = permission;
            Module = Permission.IsNullOrEmpty() ? string.Empty : Permission.Split('.')[0];
        }

        public string Permission { get; }
        public string Module { get; }
    }
}
