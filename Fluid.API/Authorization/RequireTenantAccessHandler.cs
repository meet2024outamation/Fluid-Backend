using Fluid.Entities.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fluid.API.Authorization
{
    public class RequireTenantAccessHandler : AuthorizationHandler<RequireTenantAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FluidIAMDbContext _iamContext;

        public RequireTenantAccessHandler(IHttpContextAccessor httpContextAccessor, FluidIAMDbContext iamContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _iamContext = iamContext;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireTenantAccessRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            var userPrincipalName = context.User.FindFirstValue("preferred_username")?.Replace("live.com#", "");
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                context.Fail();
                return;
            }

            // Look up user by email/UPN in IAM DB
            var user = await _iamContext.Users.FirstOrDefaultAsync(u => u.Email == userPrincipalName);
            if (user == null)
            {
                context.Fail();
                return;
            }

            // Load all roles for this user
            var userRoles = await _iamContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();

            if (!userRoles.Any())
            {
                context.Fail();
                return;
            }

            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            var projectIdHeader = httpContext.Request.Headers["X-Project-Id"].FirstOrDefault();

            // ✅ Check each role type
            foreach (var role in userRoles)
            {
                var roleName = await _iamContext.Roles
                    .Where(r => r.Id == role.RoleId)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(roleName))
                    continue;

                // Product Owner: TenantId can be null
                if (roleName == "Product Owner")
                {
                    context.Succeed(requirement);
                    return;
                }

                // Tenant Admin: TenantId required, ProjectId can be null
                if (roleName == "Tenant Admin")
                {
                    if (string.IsNullOrEmpty(tenantIdHeader))
                        continue;

                    var tenant = await _iamContext.Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdHeader);
                    if (tenant != null && role.TenantId == tenant.Id)
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }

                // Operator: TenantId + ProjectId required
                if (roleName == "Operator")
                {
                    if (string.IsNullOrEmpty(tenantIdHeader) || string.IsNullOrEmpty(projectIdHeader))
                        continue;

                    var tenant = await _iamContext.Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdHeader);
                    if (tenant == null)
                        continue;

                    if (role.TenantId == tenant.Id && role.ProjectId?.ToString() == projectIdHeader)
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }

            // If no valid role matched → Fail
            context.Fail();
        }
    }

    public class RequireTenantAccessRequirement : IAuthorizationRequirement { }
}
