using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SharedKernel.AuthorizeHandler
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;
        public PermissionAuthorizationHandler(IServiceProvider serviceProvider, ILogger<PermissionAuthorizationHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.Identity is null || !context.User.Identity.IsAuthenticated) return;

            using var scope = _serviceProvider.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            var userModulesPermissions = await permissionService.GetPermissionsAsync();

            //Platform && Module && Permission
            if (userModulesPermissions.PlatformApps.Any(p => p.AppId == context.User.FindFirstValue("aud")) && userModulesPermissions.Moduels.Contains(requirement.Module) && userModulesPermissions.Permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
                var httpContext = _serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext != null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    httpContext.Response.ContentType = "application/json";
                    httpContext.Response.WriteAsync("{\"error\": \"Unauthorized\"}").Wait();
                }
            }
        }
    }
}
