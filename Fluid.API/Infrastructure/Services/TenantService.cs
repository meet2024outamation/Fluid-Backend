using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Tenant;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantService> _logger;
    private readonly IConfiguration _configuration;

    public TenantService(
        FluidIAMDbContext iamContext,
        IServiceProvider serviceProvider,
        ILogger<TenantService> logger,
        IConfiguration configuration)
    {
        _iamContext = iamContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }
    private string BuildConnectionString(string databaseName)
    {
        var defaultConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection string not found");

        // Replace the database name in the connection string
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(defaultConnectionString)
        {
            Database = databaseName
        };

        return builder.ToString();
    }
    public async Task<Result<IEnumerable<Tenant>>> GetAllTenantsAsync()
    {
        try
        {
            var tenants = await _iamContext.Tenants
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active tenants", tenants.Count);
            return Result<IEnumerable<Tenant>>.Success(tenants, $"Retrieved {tenants.Count} tenants successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            return Result<IEnumerable<Tenant>>.Error("An error occurred while retrieving tenants.");
        }
    }

    public async Task<Result<Tenant>> GetTenantByIdAsync(string id)
    {
        try
        {
            var tenant = await _iamContext.Tenants
                .Where(t => t.Id == id && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found or inactive", id);
                return Result<Tenant>.NotFound();
            }

            _logger.LogInformation("Retrieved tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
            return Result<Tenant>.Success(tenant, "Tenant retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant with ID {TenantId}", id);
            return Result<Tenant>.Error("An error occurred while retrieving the tenant.");
        }
    }

    public async Task<Result<Tenant>> GetTenantByIdentifierAsync(string identifier)
    {
        try
        {
            var tenant = await _iamContext.Tenants
                .Where(t => t.Identifier == identifier && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                _logger.LogWarning("Tenant with identifier {TenantId} not found or inactive", identifier);
                return Result<Tenant>.NotFound();
            }

            _logger.LogInformation("Retrieved tenant {TenantId} ({TenantName}) by identifier {TenantId}",
                tenant.Id, tenant.Name, identifier);
            return Result<Tenant>.Success(tenant, "Tenant retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant with identifier {TenantId}", identifier);
            return Result<Tenant>.Error("An error occurred while retrieving the tenant.");
        }
    }

    public async Task<Result<Tenant>> CreateTenantAsync(CreateTenantRequest request)
    {
        try
        {
            // Validate unique identifier
            var existingTenant = await _iamContext.Tenants
                .Where(t => t.Identifier == request.Identifier)
                .FirstOrDefaultAsync();

            if (existingTenant != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.Identifier),
                    ErrorMessage = $"Tenant with identifier '{request.Identifier}' already exists."
                };
                return Result<Tenant>.Invalid(new List<ValidationError> { validationError });
            }
            var databaseName = !string.IsNullOrEmpty(request.DatabaseName) ? request.DatabaseName : request.Identifier;
            var connectionString = BuildConnectionString(databaseName);

            var tenant = new Tenant
            {
                Id = Guid.NewGuid().ToString(),
                Identifier = request.Identifier,
                Name = request.Name,
                Description = request.Description,
                ConnectionString = connectionString,
                DatabaseName = request.DatabaseName,
                Properties = request.Properties
            };
            tenant.CreatedDateTime = DateTime.UtcNow;
            tenant.IsActive = true;
            // Remove manual GUID generation - let PostgreSQL handle it
            // tenant.Id = Guid.NewGuid().ToString();

            _iamContext.Tenants.Add(tenant);
            await _iamContext.SaveChangesAsync();

            // Create the tenant database
            var databaseResult = await CreateTenantDatabaseAsync(tenant);
            if (!databaseResult.IsSuccess)
            {
                // Rollback tenant creation if database creation fails
                _iamContext.Tenants.Remove(tenant);
                await _iamContext.SaveChangesAsync();

                return Result<Tenant>.Error("Failed to create tenant database. Tenant creation rolled back.");
            }

            _logger.LogInformation("Successfully created tenant {TenantId} ({TenantName}) with database",
                tenant.Id, tenant.Name);

            return Result<Tenant>.Created(tenant, "Tenant created successfully with database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {TenantId}", request.Identifier);
            return Result<Tenant>.Error("An error occurred while creating the tenant.");
        }
    }

    public async Task<Result<Tenant>> UpdateTenantAsync(Tenant tenant)
    {
        try
        {
            var existingTenant = await _iamContext.Tenants
                .Where(t => t.Id == tenant.Id)
                .FirstOrDefaultAsync();

            if (existingTenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found for update", tenant.Id);
                return Result<Tenant>.NotFound();
            }

            // Check if identifier is being changed and if the new identifier already exists
            if (existingTenant.Identifier != tenant.Identifier)
            {
                var duplicateIdentifier = await _iamContext.Tenants
                    .Where(t => t.Identifier == tenant.Identifier && t.Id != tenant.Id)
                    .FirstOrDefaultAsync();

                if (duplicateIdentifier != null)
                {
                    var validationError = new ValidationError
                    {
                        Key = nameof(tenant.Identifier),
                        ErrorMessage = $"Tenant with identifier '{tenant.Identifier}' already exists."
                    };
                    return Result<Tenant>.Invalid(new List<ValidationError> { validationError });
                }
            }

            existingTenant.Identifier = tenant.Identifier;
            existingTenant.Name = tenant.Name;
            existingTenant.Description = tenant.Description;
            existingTenant.ConnectionString = tenant.ConnectionString;
            existingTenant.DatabaseName = tenant.DatabaseName;
            existingTenant.Properties = tenant.Properties;
            existingTenant.UpdatedDateTime = DateTime.UtcNow;
            existingTenant.ModifiedBy = tenant.ModifiedBy;

            await _iamContext.SaveChangesAsync();

            _logger.LogInformation("Successfully updated tenant {TenantId} ({TenantName})",
                existingTenant.Id, existingTenant.Name);

            return Result<Tenant>.Success(existingTenant, "Tenant updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenant.Id);
            return Result<Tenant>.Error("An error occurred while updating the tenant.");
        }
    }

    public async Task<Result<bool>> DeleteTenantAsync(string id)
    {
        try
        {
            var tenant = await _iamContext.Tenants
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Soft delete
            tenant.IsActive = false;
            tenant.UpdatedDateTime = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted tenant {TenantId} ({TenantName})",
                tenant.Id, tenant.Name);

            return Result<bool>.Success(true, "Tenant deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            return Result<bool>.Error("An error occurred while deleting the tenant.");
        }
    }

    public async Task<Result<bool>> CreateTenantDatabaseAsync(Tenant tenant)
    {
        try
        {
            var options = new DbContextOptionsBuilder<Fluid.Entities.Context.FluidDbContext>()
                .UseNpgsql(tenant.ConnectionString)
                .Options;

            using var context = new Fluid.Entities.Context.FluidDbContext(options, tenant);

            // Create database if it doesn't exist
            await context.Database.EnsureCreatedAsync();

            // Apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }

            _logger.LogInformation("Successfully created/updated tenant database");
            return Result<bool>.Success(true, "Tenant database created/updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant database with connection string: {ConnectionString}", tenant.ConnectionString);
            return Result<bool>.Error("An error occurred while creating the tenant database.");
        }
    }
}