using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.IAMSchema;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

/// <summary>
/// Service for managing global schemas in IAM database
/// </summary>
public class GlobalSchemaService : IGlobalSchemaService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalSchemaService> _logger;

    public GlobalSchemaService(
        FluidIAMDbContext iamContext,
        IServiceProvider serviceProvider,
        ILogger<GlobalSchemaService> logger)
    {
        _iamContext = iamContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result<GlobalSchemaResponse>> CreateAsync(CreateGlobalSchemaRequest request, int currentUserId)
    {
        try
        {
            // Validate schema name uniqueness
            var existingSchema = await _iamContext.Schemas
                .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Name.ToLower());

            if (existingSchema != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = $"Global schema with name '{request.Name}' already exists."
                };
                return Result<GlobalSchemaResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema fields
            var validationErrors = ValidateSchemaFields(request.SchemaFields);
            if (validationErrors.Any())
            {
                return Result<GlobalSchemaResponse>.Invalid(validationErrors);
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Create the schema
                var schema = new Schema
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Version = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                _iamContext.Schemas.Add(schema);
                await _iamContext.SaveChangesAsync();

                // Create schema fields
                var schemaFields = new List<SchemaField>();
                foreach (var fieldRequest in request.SchemaFields)
                {
                    var schemaField = new SchemaField
                    {
                        SchemaId = schema.Id,
                        FieldName = fieldRequest.FieldName.Trim(),
                        FieldLabel = fieldRequest.FieldLabel.Trim(),
                        DataType = fieldRequest.DataType.Trim(),
                        Format = fieldRequest.Format?.Trim(),
                        IsRequired = fieldRequest.IsRequired,
                        DisplayOrder = fieldRequest.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };

                    schemaFields.Add(schemaField);
                }

                _iamContext.SchemaFields.AddRange(schemaFields);
                await _iamContext.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the created schema with all details
                var createdSchema = await GetSchemaWithDetails(schema.Id);
                var response = MapToGlobalSchemaResponse(createdSchema!);

                _logger.LogInformation("Global schema created successfully with ID: {SchemaId}", schema.Id);
                return Result<GlobalSchemaResponse>.Created(response, "Global schema created successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating global schema: {SchemaName}", request.Name);
            return Result<GlobalSchemaResponse>.Error("An error occurred while creating the global schema.");
        }
    }

    public async Task<Result<List<GlobalSchemaListResponse>>> GetAllAsync(bool? isActive = null)
    {
        try
        {
            var query = _iamContext.Schemas
                .Include(s => s.SchemaFields)
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var schemas = await query
                .OrderBy(s => s.Name)
                .Select(s => new GlobalSchemaListResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    SchemaFieldCount = s.SchemaFields.Count,
                    CreatedBy = s.CreatedBy
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} global schemas", schemas.Count);
            return Result<List<GlobalSchemaListResponse>>.Success(schemas, $"Retrieved {schemas.Count} global schemas successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global schemas");
            return Result<List<GlobalSchemaListResponse>>.Error("An error occurred while retrieving global schemas.");
        }
    }

    public async Task<Result<GlobalSchemaResponse>> GetByIdAsync(int id)
    {
        try
        {
            var schema = await GetSchemaWithDetails(id);

            if (schema == null)
            {
                _logger.LogWarning("Global schema with ID {SchemaId} not found", id);
                return Result<GlobalSchemaResponse>.NotFound();
            }

            var response = MapToGlobalSchemaResponse(schema);

            _logger.LogInformation("Retrieved global schema with ID: {SchemaId}", id);
            return Result<GlobalSchemaResponse>.Success(response, "Global schema retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global schema with ID: {SchemaId}", id);
            return Result<GlobalSchemaResponse>.Error("An error occurred while retrieving the global schema.");
        }
    }

    public async Task<Result<GlobalSchemaResponse>> UpdateAsync(int id, CreateGlobalSchemaRequest request, int currentUserId)
    {
        try
        {
            var existingSchema = await _iamContext.Schemas
                .Include(s => s.SchemaFields)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingSchema == null)
            {
                _logger.LogWarning("Global schema with ID {SchemaId} not found for update", id);
                return Result<GlobalSchemaResponse>.NotFound();
            }

            // Check if the new name conflicts with another schema
            if (request.Name.ToLower() != existingSchema.Name.ToLower())
            {
                var duplicateName = await _iamContext.Schemas
                    .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Name.ToLower() && s.Id != id);

                if (duplicateName != null)
                {
                    var validationError = new ValidationError
                    {
                        Key = nameof(request.Name),
                        ErrorMessage = $"Global schema with name '{request.Name}' already exists."
                    };
                    return Result<GlobalSchemaResponse>.Invalid(new List<ValidationError> { validationError });
                }
            }

            // Validate schema fields
            var validationErrors = ValidateSchemaFields(request.SchemaFields);
            if (validationErrors.Any())
            {
                return Result<GlobalSchemaResponse>.Invalid(validationErrors);
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                // Update schema properties
                existingSchema.Name = request.Name.Trim();
                existingSchema.Description = request.Description?.Trim();
                existingSchema.UpdatedAt = DateTime.UtcNow;
                existingSchema.Version++;

                // Remove existing schema fields
                _iamContext.SchemaFields.RemoveRange(existingSchema.SchemaFields);

                // Add new schema fields
                var newSchemaFields = new List<SchemaField>();
                foreach (var fieldRequest in request.SchemaFields)
                {
                    var schemaField = new SchemaField
                    {
                        SchemaId = existingSchema.Id,
                        FieldName = fieldRequest.FieldName.Trim(),
                        FieldLabel = fieldRequest.FieldLabel.Trim(),
                        DataType = fieldRequest.DataType.Trim(),
                        Format = fieldRequest.Format?.Trim(),
                        IsRequired = fieldRequest.IsRequired,
                        DisplayOrder = fieldRequest.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };

                    newSchemaFields.Add(schemaField);
                }

                _iamContext.SchemaFields.AddRange(newSchemaFields);
                await _iamContext.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the updated schema with all details
                var updatedSchema = await GetSchemaWithDetails(id);
                var response = MapToGlobalSchemaResponse(updatedSchema!);

                _logger.LogInformation("Global schema updated successfully with ID: {SchemaId}", id);
                return Result<GlobalSchemaResponse>.Success(response, "Global schema updated successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global schema with ID: {SchemaId}", id);
            return Result<GlobalSchemaResponse>.Error("An error occurred while updating the global schema.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var schema = await _iamContext.Schemas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schema == null)
            {
                _logger.LogWarning("Global schema with ID {SchemaId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Soft delete
            schema.IsActive = false;
            schema.UpdatedAt = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();

            _logger.LogInformation("Global schema soft deleted successfully with ID: {SchemaId}", id);
            return Result<bool>.Success(true, "Global schema deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting global schema with ID: {SchemaId}", id);
            return Result<bool>.Error("An error occurred while deleting the global schema.");
        }
    }

    public async Task<Result<bool>> UpdateStatusAsync(int id, bool isActive)
    {
        try
        {
            var schema = await _iamContext.Schemas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schema == null)
            {
                _logger.LogWarning("Global schema with ID {SchemaId} not found for status update", id);
                return Result<bool>.NotFound();
            }

            schema.IsActive = isActive;
            schema.UpdatedAt = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();

            _logger.LogInformation("Global schema status updated successfully for ID: {SchemaId}, IsActive: {IsActive}", id, isActive);
            return Result<bool>.Success(true, "Global schema status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global schema status for ID: {SchemaId}", id);
            return Result<bool>.Error("An error occurred while updating the global schema status.");
        }
    }

    public async Task<Result<CopySchemaToTenantResponse>> CopySchemaToTenantAsync(CopySchemaToTenantRequest request, int currentUserId)
    {
        try
        {
            // Get the global schema
            var globalSchema = await GetSchemaWithDetails(request.GlobalSchemaId);
            if (globalSchema == null)
            {
                return Result<CopySchemaToTenantResponse>.NotFound();
            }

            // Get tenant information
            var tenant = await _iamContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive);

            if (tenant == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.TenantId),
                    ErrorMessage = "Tenant not found or inactive."
                };
                return Result<CopySchemaToTenantResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Create tenant-specific database context
            var tenantDbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                .UseNpgsql(tenant.ConnectionString)
                .Options;

            using var tenantContext = new FluidDbContext(tenantDbOptions, tenant);

            // Check if schema name already exists in tenant database
            var schemaName = request.CustomName ?? globalSchema.Name;
            var existingTenantSchema = await tenantContext.Schemas
                .FirstOrDefaultAsync(s => s.Name.ToLower() == schemaName.ToLower());

            if (existingTenantSchema != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.CustomName),
                    ErrorMessage = $"Schema with name '{schemaName}' already exists in tenant database."
                };
                return Result<CopySchemaToTenantResponse>.Invalid(new List<ValidationError> { validationError });
            }

            using var transaction = await tenantContext.Database.BeginTransactionAsync();

            try
            {
                // Create schema in tenant database
                var tenantSchema = new Fluid.Entities.Entities.Schema
                {
                    Name = schemaName,
                    Description = request.CustomDescription ?? globalSchema.Description,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                tenantContext.Schemas.Add(tenantSchema);
                await tenantContext.SaveChangesAsync();

                // Copy schema fields
                foreach (var globalField in globalSchema.SchemaFields.OrderBy(f => f.DisplayOrder))
                {
                    var tenantField = new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = tenantSchema.Id,
                        FieldName = globalField.FieldName,
                        FieldLabel = globalField.FieldLabel,
                        DataType = globalField.DataType,
                        Format = globalField.Format,
                        IsRequired = globalField.IsRequired,
                        DisplayOrder = globalField.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };

                    tenantContext.SchemaFields.Add(tenantField);
                }

                await tenantContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new CopySchemaToTenantResponse
                {
                    GlobalSchemaId = request.GlobalSchemaId,
                    GlobalSchemaName = globalSchema.Name,
                    TenantId = request.TenantId,
                    TenantName = tenant.Name,
                    Success = true,
                    Message = "Schema copied successfully to tenant",
                    TenantSchemaId = tenantSchema.Id
                };

                _logger.LogInformation("Global schema {GlobalSchemaId} copied to tenant {TenantId} as schema {TenantSchemaId}",
                    request.GlobalSchemaId, request.TenantId, tenantSchema.Id);

                return Result<CopySchemaToTenantResponse>.Success(response, "Schema copied to tenant successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying global schema {GlobalSchemaId} to tenant {TenantId}",
                request.GlobalSchemaId, request.TenantId);

            var response = new CopySchemaToTenantResponse
            {
                GlobalSchemaId = request.GlobalSchemaId,
                TenantId = request.TenantId,
                Success = false,
                Message = $"Failed to copy schema to tenant: {ex.Message}"
            };

            return Result<CopySchemaToTenantResponse>.Error("An error occurred while copying schema to tenant.");
        }
    }

    public async Task<Result<List<GlobalSchemaListResponse>>> SearchAsync(string searchTerm, bool? isActive = null)
    {
        try
        {
            var query = _iamContext.Schemas
                .Include(s => s.SchemaFields)
                .Where(s => s.Name.Contains(searchTerm) || (s.Description != null && s.Description.Contains(searchTerm)))
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var schemas = await query
                .OrderBy(s => s.Name)
                .Select(s => new GlobalSchemaListResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    SchemaFieldCount = s.SchemaFields.Count,
                    CreatedBy = s.CreatedBy
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} global schemas matching search term: {SearchTerm}", schemas.Count, searchTerm);
            return Result<List<GlobalSchemaListResponse>>.Success(schemas, $"Found {schemas.Count} matching global schemas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching global schemas with term: {SearchTerm}", searchTerm);
            return Result<List<GlobalSchemaListResponse>>.Error("An error occurred while searching global schemas.");
        }
    }

    #region Private Helper Methods

    private async Task<Schema?> GetSchemaWithDetails(int id)
    {
        return await _iamContext.Schemas
            .Include(s => s.SchemaFields.OrderBy(f => f.DisplayOrder))
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    private static GlobalSchemaResponse MapToGlobalSchemaResponse(Schema schema)
    {
        return new GlobalSchemaResponse
        {
            Id = schema.Id,
            Name = schema.Name,
            Description = schema.Description,
            Version = schema.Version,
            IsActive = schema.IsActive,
            CreatedAt = schema.CreatedAt,
            UpdatedAt = schema.UpdatedAt,
            CreatedBy = schema.CreatedBy,
            SchemaFields = schema.SchemaFields.Select(f => new GlobalSchemaFieldResponse
            {
                Id = f.Id,
                SchemaId = f.SchemaId,
                FieldName = f.FieldName,
                FieldLabel = f.FieldLabel,
                DataType = f.DataType,
                Format = f.Format,
                IsRequired = f.IsRequired,
                DisplayOrder = f.DisplayOrder,
                CreatedAt = f.CreatedAt
            }).OrderBy(f => f.DisplayOrder).ToList()
        };
    }

    private static List<ValidationError> ValidateSchemaFields(List<CreateGlobalSchemaFieldRequest> schemaFields)
    {
        var errors = new List<ValidationError>();

        if (!schemaFields.Any())
        {
            errors.Add(new ValidationError
            {
                Key = "SchemaFields",
                ErrorMessage = "At least one schema field is required."
            });
            return errors;
        }

        // Check for duplicate field names
        var duplicateFieldNames = schemaFields
            .GroupBy(f => f.FieldName.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateName in duplicateFieldNames)
        {
            errors.Add(new ValidationError
            {
                Key = "SchemaFields",
                ErrorMessage = $"Duplicate field name found: '{duplicateName}'"
            });
        }

        // Check for duplicate display orders
        var duplicateDisplayOrders = schemaFields
            .GroupBy(f => f.DisplayOrder)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateOrder in duplicateDisplayOrders)
        {
            errors.Add(new ValidationError
            {
                Key = "SchemaFields",
                ErrorMessage = $"Duplicate display order found: {duplicateOrder}"
            });
        }

        return errors;
    }

    #endregion
}