using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.AuthorizeHandler
{
    public static class AuthorizeStartUp
    {
       public static void ConfigureAuthorization(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
            builder.Services.AddTransient<IPermissionService, PermissionService>();
        }
    }
}
