using Finbuckle.MultiTenant.Abstractions;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;

namespace Fluid.API.Helpers;

/// <summary>
/// Helper class for applying EF Core migrations across all tenants
/// </summary>
public static class MigrationHelper
{
    /// <summary>
    /// Applies pending migrations to all tenant databases
    /// </summary>
    /// <param name="services">Service provider for dependency resolution</param>
    /// <param name="logger">Logger for migration status reporting</param>
    public static async Task ApplyMigrationsAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            logger.LogInformation("?? Starting multi-tenant migration process...");

            using var scope = services.CreateScope();

            // Get tenant store
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<Tenant>>();
            
            logger.LogInformation("?? Retrieving all active tenants...");
            var tenants = await tenantStore.GetAllAsync();

            if (!tenants.Any())
            {
                logger.LogInformation("?? No tenants found in the system");
                return;
            }

            logger.LogInformation("?? Found {TenantCount} tenants to process", tenants.Count());

            int successCount = 0;
            int errorCount = 0;

            foreach (var tenant in tenants.Where(t => t.IsActive))
            {
                try
                {
                    logger.LogInformation("?? Processing tenant: {TenantName} ({TenantId})", 
                        tenant.Name, tenant.Identifier);

                    if (string.IsNullOrEmpty(tenant.ConnectionString))
                    {
                        logger.LogWarning("?? Tenant {TenantName} has no connection string, skipping...", 
                            tenant.Name);
                        continue;
                    }

                    // Create tenant-specific database context
                    var dbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                        .UseNpgsql(tenant.ConnectionString)
                        .UseSnakeCaseNamingConvention()
                        .Options;

                    using var tenantContext = new FluidDbContext(dbOptions);

                    // Check for pending migrations
                    var pendingMigrations = await tenantContext.Database.GetPendingMigrationsAsync();
                    
                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("?? Applying {MigrationCount} pending migrations for tenant: {TenantName}", 
                            pendingMigrations.Count(), tenant.Name);

                        // Log migration names for debugging
                        foreach (var migration in pendingMigrations)
                        {
                            logger.LogDebug("  - {MigrationName}", migration);
                        }

                        // Apply migrations
                        await tenantContext.Database.MigrateAsync();
                        
                        logger.LogInformation("? Successfully applied migrations for tenant: {TenantName}", 
                            tenant.Name);
                        successCount++;
                    }
                    else
                    {
                        logger.LogInformation("? Tenant {TenantName} is up to date", tenant.Name);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "? Failed to apply migrations for tenant: {TenantName} - {ErrorMessage}", 
                        tenant.Name, ex.Message);
                    errorCount++;

                    // Continue with other tenants even if one fails
                    continue;
                }
            }

            // Summary report
            logger.LogInformation("?? Migration process completed:");
            logger.LogInformation("   ? Successful: {SuccessCount} tenants", successCount);
            
            if (errorCount > 0)
            {
                logger.LogWarning("   ? Failed: {ErrorCount} tenants", errorCount);
            }
            else
            {
                logger.LogInformation("   ? Failed: 0 tenants");
            }

            logger.LogInformation("?? Multi-tenant migration process finished!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "?? Critical error during multi-tenant migration process: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Ensures tenant database exists and creates it if it doesn't
    /// </summary>
    /// <param name="connectionString">Tenant database connection string</param>
    /// <param name="tenantName">Tenant name for logging</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>True if database exists or was created successfully</returns>
    public static async Task<bool> EnsureTenantDatabaseExistsAsync(
        string connectionString, 
        string tenantName, 
        ILogger logger)
    {
        try
        {
            var dbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

            using var context = new FluidDbContext(dbOptions);

            logger.LogInformation("?? Checking database existence for tenant: {TenantName}", tenantName);

            // This will create the database if it doesn't exist
            var created = await context.Database.EnsureCreatedAsync();

            if (created)
            {
                logger.LogInformation("?? Created new database for tenant: {TenantName}", tenantName);
            }
            else
            {
                logger.LogInformation("? Database already exists for tenant: {TenantName}", tenantName);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? Failed to ensure database exists for tenant: {TenantName} - {ErrorMessage}", 
                tenantName, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Applies migrations to a specific tenant by tenant identifier
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="tenantIdentifier">Tenant identifier</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>True if migrations were applied successfully</returns>
    public static async Task<bool> ApplyMigrationsForTenantAsync(
        IServiceProvider services, 
        string tenantIdentifier, 
        ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<Tenant>>();

            var tenant = await tenantStore.TryGetAsync(tenantIdentifier);
            if (tenant == null)
            {
                logger.LogWarning("?? Tenant not found: {TenantIdentifier}", tenantIdentifier);
                return false;
            }

            if (string.IsNullOrEmpty(tenant.ConnectionString))
            {
                logger.LogWarning("?? Tenant {TenantIdentifier} has no connection string", tenantIdentifier);
                return false;
            }

            logger.LogInformation("?? Applying migrations for specific tenant: {TenantName}", tenant.Name);

            var dbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                .UseNpgsql(tenant.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

            using var context = new FluidDbContext(dbOptions);

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("?? Applying {MigrationCount} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("? Migrations applied successfully for tenant: {TenantName}", tenant.Name);
            }
            else
            {
                logger.LogInformation("? Tenant {TenantName} is already up to date", tenant.Name);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? Failed to apply migrations for tenant {TenantIdentifier}: {ErrorMessage}", 
                tenantIdentifier, ex.Message);
            return false;
        }
    }
}