using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Fluid.API.Authorization;

/// <summary>
/// Policy provider that creates dynamic authorization policies for permissions
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if this is a permission-based policy (contains comma-separated permissions)
        if (policyName.Contains(',') || IsKnownPermission(policyName))
        {
            var permissions = policyName.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => p.Trim())
                                      .ToArray();

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permissions))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to default policy provider for other policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    private static bool IsKnownPermission(string policyName)
    {
        // Check if the policy name matches any of our known permissions
        var permissionType = typeof(ApplicationPermissions);
        var permissionConstants = permissionType.GetFields()
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissionConstants.Contains(policyName);
    }
}