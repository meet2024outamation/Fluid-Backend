using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Fluid.API.Endpoints.Auth;

[Route("api/auth")]
//[Authorize]
public class TestAuth : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<AuthTestResponse>
{
    [HttpGet("test")]
    [SwaggerOperation(
        Summary = "Test authentication",
        Description = "Tests if authentication is working and returns token claims information",
        OperationId = "Auth.Test",
        Tags = new[] { "Authentication" })
    ]
    [SwaggerResponse(200, "Authentication test successful", typeof(AuthTestResponse))]
    [SwaggerResponse(401, "Unauthorized - Authentication failed")]
    public async override Task<ActionResult<AuthTestResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var response = new AuthTestResponse
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType ?? "None",
            UserName = User.Identity?.Name ?? "Unknown",
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Not found",
            Email = User.FindFirstValue(ClaimTypes.Email) ??
                   User.FindFirstValue("email") ??
                   User.FindFirstValue("preferred_username") ?? "Not found",
            Claims = User.Claims.Select(c => new ClaimInfo
            {
                Type = c.Type,
                Value = c.Value
            }).ToList(),
            TokenIssuer = User.FindFirstValue("iss") ?? "Not found",
            TokenAudience = User.FindFirstValue("aud") ?? "Not found"
        };

        return Ok(response);
    }
}

public class AuthTestResponse
{
    public bool IsAuthenticated { get; set; }
    public string AuthenticationType { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TokenIssuer { get; set; } = string.Empty;
    public string TokenAudience { get; set; } = string.Empty;
    public List<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();
}

public class ClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}