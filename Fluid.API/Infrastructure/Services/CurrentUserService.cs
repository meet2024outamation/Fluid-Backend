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
            var principal = httpContext.User;

            // 1) Some systems inject app user id as integer in NameIdentifier
            var nameIdentifier = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameIdentifier) && int.TryParse(nameIdentifier, out var appUserId))
            {
                _logger.LogDebug("Current user ID from NameIdentifier (int) claim: {UserId}", appUserId);
                return appUserId;
            }

            // 2) Azure AD Object Id (oid) -> map to IAM.Users.AzureAdId
            //    Standard types: "oid" or "http://schemas.microsoft.com/identity/claims/objectidentifier"
            var objectId = principal.FindFirst("oid")?.Value
                         ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (!string.IsNullOrWhiteSpace(objectId))
            {
                var userByOid = _iamContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.AzureAdId == objectId);
                if (userByOid != null)
                {
                    _logger.LogDebug("Current user ID from Azure AD ObjectId (oid) lookup: {UserId}", userByOid.Id);
                    return userByOid.Id;
                }
            }

            // 3) Fall back to email-based lookup: preferred_username / emails / upn
            var email = principal.FindFirst("preferred_username")?.Value
                     ?? principal.FindFirst(ClaimTypes.Email)?.Value
                     ?? principal.FindFirst("emails")?.Value
                     ?? principal.FindFirst("upn")?.Value
                     ?? principal.FindFirst("unique_name")?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var userByEmail = _iamContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
                if (userByEmail != null)
                {
                    _logger.LogDebug("Current user ID from email lookup: {UserId} ({Email})", userByEmail.Id, email);
                    return userByEmail.Id;
                }
            }

            _logger.LogWarning(
                "Could not determine user ID from claims. NameIdentifier: {NameIdentifier}, OID: {Oid}, Email: {Email}",
                nameIdentifier, objectId, email);
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