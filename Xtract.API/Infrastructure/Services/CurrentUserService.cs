using Xtract.API.Infrastructure.Interfaces;

namespace Xtract.API.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public int GetCurrentUserId()
    {
        // TODO: In a real application, this would extract the user ID from JWT token or claims
        // For now, returning default user ID
        var userId = 1; // Default system user
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Extract user ID from claims when authentication is implemented
            // var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
            // if (int.TryParse(userIdClaim, out var parsedUserId))
            // {
            //     userId = parsedUserId;
            // }
        }

        _logger.LogDebug("Current user ID: {UserId}", userId);
        return userId;
    }

    public string GetCurrentUserName()
    {
        // TODO: In a real application, this would extract the user name from JWT token or claims
        // For now, returning default user name
        var userName = "System User";
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Extract user name from claims when authentication is implemented
            // userName = httpContext.User.FindFirst("name")?.Value ?? "Unknown User";
        }

        _logger.LogDebug("Current user name: {UserName}", userName);
        return userName;
    }
}