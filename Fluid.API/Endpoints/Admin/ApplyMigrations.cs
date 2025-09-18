using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Admin;

/// <summary>
/// Administrative endpoint for applying database migrations across all tenants
/// </summary>
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class ApplyMigrations : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<MigrationResult>
{
    private readonly ILogger<ApplyMigrations> _logger;

    public ApplyMigrations(ILogger<ApplyMigrations> logger)
    {
        _logger = logger;
    }

    [HttpPost("apply-migrations")]
    [SwaggerOperation(
        Summary = "Apply pending migrations to all tenants",
        Description = "Applies any pending EF Core migrations to all active tenant databases. Requires Product Owner role.",
        OperationId = "Admin.ApplyMigrations",
        Tags = new[] { "Administration" })
    ]
    [SwaggerResponse(200, "Migrations applied successfully", typeof(MigrationResult))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(500, "Internal server error during migration process")]
    public async override Task<ActionResult<MigrationResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("?? Manual migration process initiated by admin user");

            var startTime = DateTime.UtcNow;

            // Apply migrations to all tenants
            await MigrationHelper.ApplyMigrationsAsync(HttpContext.RequestServices, _logger);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var result = new MigrationResult
            {
                Success = true,
                Message = "Migrations applied successfully to all tenants",
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("? Manual migration process completed successfully in {Duration}", 
                duration.ToString(@"mm\:ss\.fff"));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Manual migration process failed: {ErrorMessage}", ex.Message);

            var result = new MigrationResult
            {
                Success = false,
                Message = $"Migration process failed: {ex.Message}",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Duration = TimeSpan.Zero,
                ProcessedAt = DateTime.UtcNow,
                ErrorDetails = ex.ToString()
            };

            return StatusCode(500, result);
        }
    }
}

/// <summary>
/// Result of migration operation
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ErrorDetails { get; set; }
}