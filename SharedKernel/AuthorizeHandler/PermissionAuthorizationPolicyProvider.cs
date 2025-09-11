using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SharedKernel.AuthorizeHandler
{
    public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            AuthorizationPolicy? authorizationPolicy = await base.GetPolicyAsync(policyName);
            if (authorizationPolicy is not null)
                return authorizationPolicy;
            return new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(policyName)).Build();
        }
    }
}
