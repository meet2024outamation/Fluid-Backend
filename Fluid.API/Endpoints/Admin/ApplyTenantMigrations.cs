using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Admin;

/// <summary>
/// Request model for applying migrations to a specific tenant
/// </summary>
public class ApplyTenantMigrationsRequest
{
    /// <summary>
    /// Tenant identifier for which to apply migrations
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Administrative endpoint for applying database migrations to a specific tenant
/// </summary>
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class ApplyTenantMigrations : EndpointBaseAsync
    .WithRequest<ApplyTenantMigrationsRequest>
    .WithActionResult<MigrationResult>
{
    private readonly ILogger<ApplyTenantMigrations> _logger;

    public ApplyTenantMigrations(ILogger<ApplyTenantMigrations> logger)
    {
        _logger = logger;
    }

    [HttpPost("apply-migrations/{tenantIdentifier}")]
    [SwaggerOperation(
        Summary = "Apply pending migrations to a specific tenant",
        Description = "Applies any pending EF Core migrations to a specific tenant database. Requires Product Owner role.",
        OperationId = "Admin.ApplyTenantMigrations",
        Tags = new[] { "Administration" })
    ]
    [SwaggerResponse(200, "Migrations applied successfully to tenant", typeof(MigrationResult))]
    [SwaggerResponse(400, "Invalid tenant identifier")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Tenant not found")]
    [SwaggerResponse(500, "Internal server error during migration process")]
    public async override Task<ActionResult<MigrationResult>> HandleAsync(
        ApplyTenantMigrationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return BadRequest("Tenant identifier is required");
        }

        try
        {
            _logger.LogInformation("?? Manual migration process initiated for tenant: {TenantId}",
                request.TenantId);

            var startTime = DateTime.UtcNow;

            // Apply migrations to specific tenant
            var success = await MigrationHelper.ApplyMigrationsForTenantAsync(
                HttpContext.RequestServices,
                request.TenantId,
                _logger);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            if (success)
            {
                var result = new MigrationResult
                {
                    Success = true,
                    Message = $"Migrations applied successfully to tenant: {request.TenantId}",
                    StartTime = startTime,
                    EndTime = endTime,
                    Duration = duration,
                    ProcessedAt = DateTime.UtcNow
                };

                _logger.LogInformation("? Manual migration process completed successfully for tenant {TenantId} in {Duration}",
                    request.TenantId, duration.ToString(@"mm\:ss\.fff"));

                return Ok(result);
            }
            else
            {
                var result = new MigrationResult
                {
                    Success = false,
                    Message = $"Failed to apply migrations to tenant: {request.TenantId}. Tenant may not exist or have no connection string.",
                    StartTime = startTime,
                    EndTime = endTime,
                    Duration = duration,
                    ProcessedAt = DateTime.UtcNow
                };

                return NotFound(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Manual migration process failed for tenant {TenantId}: {ErrorMessage}",
                request.TenantId, ex.Message);

            var result = new MigrationResult
            {
                Success = false,
                Message = $"Migration process failed for tenant {request.TenantId}: {ex.Message}",
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