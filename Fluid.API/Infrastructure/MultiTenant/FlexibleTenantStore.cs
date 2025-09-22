using Finbuckle.MultiTenant.Abstractions;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fluid.API.Infrastructure.MultiTenant;

/// <summary>
/// Custom tenant store that can lookup tenants by either Id or Identifier
/// This allows flexibility in how the X-Tenant-Id header is interpreted
/// </summary>
public class FlexibleTenantStore : IMultiTenantStore<Tenant>
{
    private readonly FluidIAMDbContext _context;
    private readonly ILogger<FlexibleTenantStore> _logger;

    public FlexibleTenantStore(FluidIAMDbContext context, ILogger<FlexibleTenantStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Tenant?> TryGetAsync(string identifier)
    {
        try
        {
            _logger.LogInformation("?? FlexibleTenantStore.TryGetAsync called with identifier: '{Identifier}'", identifier);

            // First try to find by Identifier (standard Finbuckle approach)
            var tenant = await _context.Tenants
                .Where(t => t.Identifier == identifier && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant != null)
            {
                _logger.LogInformation("? Found tenant by Identifier: {TenantId} ({TenantName}) - ConnectionString DB: {Database}", 
                    tenant.Id, tenant.Name, GetDatabaseName(tenant.ConnectionString));
                return tenant;
            }

            // If not found by Identifier, try by Id (for backward compatibility)
            tenant = await _context.Tenants
                .Where(t => t.Id == identifier && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant != null)
            {
                _logger.LogInformation("? Found tenant by Id: {TenantId} ({TenantName}) - Identifier: {Identifier} - ConnectionString DB: {Database}", 
                    tenant.Id, tenant.Name, tenant.Identifier, GetDatabaseName(tenant.ConnectionString));
                return tenant;
            }

            _logger.LogWarning("? No tenant found with identifier or id: {Identifier}", identifier);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "?? Error looking up tenant with identifier: {Identifier}", identifier);
            return null;
        }
    }

    private string GetDatabaseName(string connectionString)
    {
        try
        {
            if (string.IsNullOrEmpty(connectionString)) return "no-connection-string";
            
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.Database ?? "no-database-specified";
        }
        catch
        {
            return "invalid-connection-string-format";
        }
    }

    public async Task<Tenant?> TryGetByIdentifierAsync(string identifier)
    {
        return await TryGetAsync(identifier);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        try
        {
            return await _context.Tenants
                .Where(t => t.IsActive)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            return Enumerable.Empty<Tenant>();
        }
    }

    // Implement the overload with offset and limit
    public async Task<IEnumerable<Tenant>> GetAllAsync(int offset, int limit)
    {
        try
        {
            return await _context.Tenants
                .Where(t => t.IsActive)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants with offset {Offset} and limit {Limit}", offset, limit);
            return Enumerable.Empty<Tenant>();
        }
    }

    public async Task<bool> TryAddAsync(Tenant tenantInfo)
    {
        try
        {
            _context.Tenants.Add(tenantInfo);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tenant: {TenantId}", tenantInfo.Id);
            return false;
        }
    }

    public async Task<bool> TryRemoveAsync(string identifier)
    {
        try
        {
            var tenant = await TryGetAsync(identifier);
            if (tenant != null)
            {
                // Soft delete - set IsActive to false
                tenant.IsActive = false;
                tenant.UpdatedDateTime = DateTime.UtcNow;
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tenant: {Identifier}", identifier);
            return false;
        }
    }

    public async Task<bool> TryUpdateAsync(Tenant tenantInfo)
    {
        try
        {
            var existingTenant = await TryGetAsync(tenantInfo.Identifier);
            if (existingTenant != null)
            {
                _context.Entry(existingTenant).CurrentValues.SetValues(tenantInfo);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant: {TenantId}", tenantInfo.Id);
            return false;
        }
    }
}