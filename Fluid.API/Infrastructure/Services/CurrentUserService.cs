using Fluid.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Fluid.Entities.Context;
using System.Security.Claims;

namespace Fluid.API.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;
    private readonly FluidIAMDbContext _iamContext;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor, 
        ILogger<CurrentUserService> logger,
        FluidIAMDbContext iamContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _iamContext = iamContext;
    }

    public int GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return 0; // Return 0 for unauthenticated users
        }

        try
        {
            // Try to get user ID directly from claims first
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogDebug("Current user ID from NameIdentifier claim: {UserId}", userId);
                return userId;
            }

            // If direct user ID not available, try to find user by Azure AD ID
            var azureAdIdClaim = httpContext.User.FindFirst("preferred_username")?.Value 
                               ?? httpContext.User.FindFirst("upn")?.Value
                               ?? httpContext.User.FindFirst("unique_name")?.Value;

            if (!string.IsNullOrEmpty(azureAdIdClaim))
            {
                var user = _iamContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.AzureAdId == azureAdIdClaim);

                if (user != null)
                {
                    _logger.LogDebug("Current user ID from Azure AD lookup: {UserId} for AzureAdId: {AzureAdId}", 
                        user.Id, azureAdIdClaim);
                    return user.Id;
                }
            }

            _logger.LogWarning("Could not determine user ID from claims. UserIdClaim: {UserIdClaim}, AzureAdIdClaim: {AzureAdIdClaim}", 
                userIdClaim, azureAdIdClaim);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID");
            return 0;
        }
    }

    public string GetCurrentUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return "Anonymous";
        }

        try
        {
            // Try to get user name from various claims
            var userName = httpContext.User.FindFirst("name")?.Value
                         ?? httpContext.User.FindFirst("given_name")?.Value
                         ?? httpContext.User.FindFirst("preferred_username")?.Value
                         ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                         ?? "Unknown User";

            _logger.LogDebug("Current user name: {UserName}", userName);
            return userName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user name");
            return "Unknown User";
        }
    }
}