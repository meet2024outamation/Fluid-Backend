using Fluid.API.Constants;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class SchemaService : ISchemaService
{
    private readonly FluidDbContext _context;
    private readonly ILogger<SchemaService> _logger;

    public SchemaService(FluidDbContext context, ILogger<SchemaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<SchemaResponse>> CreateAsync(CreateSchemaRequest request, int currentUserId)
    {
        try
        {
            // Validate schema name uniqueness
            var existingSchema = await _context.Schemas
                .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Name.ToLower());

            if (existingSchema != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = $"Schema with name '{request.Name}' already exists."
                };
                return Result<SchemaResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema fields
            var validationErrors = ValidateSchemaFields(request.SchemaFields);
            if (validationErrors.Any())
            {
                return Result<SchemaResponse>.Invalid(validationErrors);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create the schema
                var schema = new Entities.Entities.Schema
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Version = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                _context.Schemas.Add(schema);
                await _context.SaveChangesAsync();

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

                _context.SchemaFields.AddRange(schemaFields);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the created schema with all details
                var createdSchema = await GetSchemaWithDetails(schema.Id);
                var response = MapToSchemaResponse(createdSchema!);

                _logger.LogInformation("Schema created successfully with ID: {SchemaId}", schema.Id);
                return Result<SchemaResponse>.Created(response, "Schema created successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schema with name: {SchemaName}", request.Name);
            return Result<SchemaResponse>.Error("An error occurred while creating the schema.");
        }
    }

    public async Task<Result<List<SchemaListResponse>>> GetAllAsync(int? projectId = null)
    {
        try
        {
            var query = _context.Schemas
                .Include(s => s.CreatedByUser)
                .Include(s => s.SchemaFields)
                .AsQueryable();

            // Filter by projectId if provided
            if (projectId.HasValue)
            {
                // Validate project exists
                var projectExists = await _context.Projects
                    .AnyAsync(c => c.Id == projectId.Value);

                if (!projectExists)
                {
                    var validationError = new ValidationError
                    {
                        Key = nameof(projectId),
                        ErrorMessage = "Project not found."
                    };
                    return Result<List<SchemaListResponse>>.Invalid(new List<ValidationError> { validationError });
                }

                // Filter schemas assigned to the project
                query = query.Where(s => s.ProjectSchemas.Any(cs => cs.ProjectId == projectId.Value));
            }

            var schemas = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            var responses = schemas.Select(s => new SchemaListResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Version = s.Version,
                IsActive = s.IsActive,
                SchemaFieldCount = s.SchemaFields.Count,
                CreatedAt = s.CreatedAt,
                CreatedByName = s.CreatedByUser.Name
            }).ToList();

            var message = projectId.HasValue 
                ? $"Retrieved {responses.Count} schemas for project {projectId.Value} successfully"
                : $"Retrieved {responses.Count} schemas successfully";

            _logger.LogInformation("Retrieved {Count} schemas{ProjectFilter}", responses.Count, 
                projectId.HasValue ? $" for project {projectId.Value}" : "");
            
            return Result<List<SchemaListResponse>>.Success(responses, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schemas{ProjectFilter}", 
                projectId.HasValue ? $" for project {projectId.Value}" : "");
            return Result<List<SchemaListResponse>>.Error("An error occurred while retrieving schemas.");
        }
    }

    public async Task<Result<SchemaResponse>> GetByIdAsync(int id)
    {
        try
        {
            var schema = await GetSchemaWithDetails(id);

            if (schema == null)
            {
                _logger.LogWarning("Schema with ID {SchemaId} not found", id);
                return Result<SchemaResponse>.NotFound();
            }

            var response = MapToSchemaResponse(schema);

            _logger.LogInformation("Retrieved schema with ID: {SchemaId}", id);
            return Result<SchemaResponse>.Success(response, "Schema retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema with ID: {SchemaId}", id);
            return Result<SchemaResponse>.Error("An error occurred while retrieving the schema.");
        }
    }

    public async Task<Result<SchemaResponse>> UpdateAsync(int id, CreateSchemaRequest request, int currentUserId)
    {
        try
        {
            var schema = await GetSchemaWithDetails(id);

            if (schema == null)
            {
                _logger.LogWarning("Schema with ID {SchemaId} not found for update", id);
                return Result<SchemaResponse>.NotFound();
            }

            // Validate schema name uniqueness (exclude current schema)
            var existingSchema = await _context.Schemas
                .FirstOrDefaultAsync(s => s.Id != id && s.Name.ToLower() == request.Name.ToLower());

            if (existingSchema != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = $"Schema with name '{request.Name}' already exists."
                };
                return Result<SchemaResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema fields
            var validationErrors = ValidateUpdateSchemaFields(request.SchemaFields);
            if (validationErrors.Any())
            {
                return Result<SchemaResponse>.Invalid(validationErrors);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Update schema
                schema.Name = request.Name.Trim();
                schema.Description = request.Description?.Trim();
                schema.UpdatedAt = DateTime.UtcNow;
                schema.Version++;

                // Remove existing schema fields
                _context.SchemaFields.RemoveRange(schema.SchemaFields);

                // Add updated schema fields
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

                _context.SchemaFields.AddRange(schemaFields);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the updated schema with all details
                var updatedSchema = await GetSchemaWithDetails(schema.Id);
                var response = MapToSchemaResponse(updatedSchema!);

                _logger.LogInformation("Schema updated successfully with ID: {SchemaId}", schema.Id);
                return Result<SchemaResponse>.Success(response, "Schema updated successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema with ID: {SchemaId}", id);
            return Result<SchemaResponse>.Error("An error occurred while updating the schema.");
        }
    }

    public async Task<Result<SchemaResponse>> UpdateStatusAsync(int id, UpdateSchemaStatusRequest request, int currentUserId)
    {
        try
        {
            var schema = await GetSchemaWithDetails(id);

            if (schema == null)
            {
                _logger.LogWarning("Schema with ID {SchemaId} not found for status update", id);
                return Result<SchemaResponse>.NotFound();
            }

            schema.IsActive = request.IsActive;
            schema.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = MapToSchemaResponse(schema);

            _logger.LogInformation("Schema status updated successfully for ID: {SchemaId}, IsActive: {IsActive}", id, request.IsActive);
            return Result<SchemaResponse>.Success(response, "Schema status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema status for ID: {SchemaId}", id);
            return Result<SchemaResponse>.Error("An error occurred while updating the schema status.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var schema = await _context.Schemas
                .Include(s => s.SchemaFields)
                .Include(s => s.FieldMappings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schema == null)
            {
                _logger.LogWarning("Schema with ID {SchemaId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Check if schema is being used by field mappings
            if (schema.FieldMappings.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Schema",
                    ErrorMessage = "Cannot delete schema as it is being used by field mappings. Please remove all field mappings first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Remove schema fields first (cascade delete should handle this, but being explicit)
            _context.SchemaFields.RemoveRange(schema.SchemaFields);

            // Remove the schema
            _context.Schemas.Remove(schema);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Schema deleted successfully with ID: {SchemaId}", id);
            return Result<bool>.Success(true, "Schema deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schema with ID: {SchemaId}", id);
            return Result<bool>.Error("An error occurred while deleting the schema.");
        }
    }

    private List<ValidationError> ValidateSchemaFields(List<CreateSchemaFieldRequest> schemaFields)
    {
        var validationErrors = new List<ValidationError>();

        // Check for duplicate field names
        var duplicateFieldNames = schemaFields
            .GroupBy(f => f.FieldName.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateFieldName in duplicateFieldNames)
        {
            validationErrors.Add(new ValidationError
            {
                Key = nameof(CreateSchemaRequest.SchemaFields),
                ErrorMessage = $"Duplicate field name '{duplicateFieldName}' found in schema fields."
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
            validationErrors.Add(new ValidationError
            {
                Key = nameof(CreateSchemaRequest.SchemaFields),
                ErrorMessage = $"Duplicate display order '{duplicateOrder}' found in schema fields."
            });
        }

        // Validate individual fields
        for (int i = 0; i < schemaFields.Count; i++)
        {
            var field = schemaFields[i];

            // Validate field name format (no spaces, alphanumeric + underscore)
            if (!System.Text.RegularExpressions.Regex.IsMatch(field.FieldName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                validationErrors.Add(new ValidationError
                {
                    Key = $"SchemaFields[{i}].FieldName",
                    ErrorMessage = $"Field name '{field.FieldName}' must start with a letter and contain only letters, numbers, and underscores."
                });
            }

            // Validate DataType is supported
            if (!SchemaFieldDataTypes.IsSupported(field.DataType))
            {
                validationErrors.Add(new ValidationError
                {
                    Key = $"SchemaFields[{i}].DataType",
                    ErrorMessage = $"Data type '{field.DataType}' is not supported. Supported types: {string.Join(", ", SchemaFieldDataTypes.All)}"
                });
            }

            // Validate format for specific data types
            if (string.Equals(field.DataType, SchemaFieldDataTypes.Date, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(field.Format))
            {
                var validDateFormats = new[] { "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "MM-dd-yyyy" };
                if (!validDateFormats.Contains(field.Format))
                {
                    validationErrors.Add(new ValidationError
                    {
                        Key = $"SchemaFields[{i}].Format",
                        ErrorMessage = $"Invalid date format '{field.Format}'. Valid formats: {string.Join(", ", validDateFormats)}"
                    });
                }
            }
        }

        return validationErrors;
    }

    private List<ValidationError> ValidateUpdateSchemaFields(List<CreateSchemaFieldRequest> schemaFields)
    {
        var validationErrors = new List<ValidationError>();

        // Check for duplicate field names
        var duplicateFieldNames = schemaFields
            .GroupBy(f => f.FieldName.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateFieldName in duplicateFieldNames)
        {
            validationErrors.Add(new ValidationError
            {
                Key = nameof(CreateSchemaRequest.SchemaFields),
                ErrorMessage = $"Duplicate field name '{duplicateFieldName}' found in schema fields."
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
            validationErrors.Add(new ValidationError
            {
                Key = nameof(CreateSchemaRequest.SchemaFields),
                ErrorMessage = $"Duplicate display order '{duplicateOrder}' found in schema fields."
            });
        }

        // Validate individual fields
        for (int i = 0; i < schemaFields.Count; i++)
        {
            var field = schemaFields[i];

            // Validate field name format (no spaces, alphanumeric + underscore)
            if (!System.Text.RegularExpressions.Regex.IsMatch(field.FieldName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                validationErrors.Add(new ValidationError
                {
                    Key = $"SchemaFields[{i}].FieldName",
                    ErrorMessage = $"Field name '{field.FieldName}' must start with a letter and contain only letters, numbers, and underscores."
                });
            }

            // Validate DataType is supported
            if (!SchemaFieldDataTypes.IsSupported(field.DataType))
            {
                validationErrors.Add(new ValidationError
                {
                    Key = $"SchemaFields[{i}].DataType",
                    ErrorMessage = $"Data type '{field.DataType}' is not supported. Supported types: {string.Join(", ", SchemaFieldDataTypes.All)}"
                });
            }

            // Validate format for specific data types
            if (string.Equals(field.DataType, SchemaFieldDataTypes.Date, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(field.Format))
            {
                var validDateFormats = new[] { "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "MM-dd-yyyy" };
                if (!validDateFormats.Contains(field.Format))
                {
                    validationErrors.Add(new ValidationError
                    {
                        Key = $"SchemaFields[{i}].Format",
                        ErrorMessage = $"Invalid date format '{field.Format}'. Valid formats: {string.Join(", ", validDateFormats)}"
                    });
                }
            }
        }

        return validationErrors;
    }

    private async Task<Entities.Entities.Schema?> GetSchemaWithDetails(int schemaId)
    {
        return await _context.Schemas
            .Include(s => s.CreatedByUser)
            .Include(s => s.SchemaFields.OrderBy(sf => sf.DisplayOrder))
            .FirstOrDefaultAsync(s => s.Id == schemaId);
    }

    private static SchemaResponse MapToSchemaResponse(Entities.Entities.Schema schema)
    {
        var schemaFields = schema.SchemaFields.Select(sf => new SchemaFieldResponse
        {
            Id = sf.Id,
            FieldName = sf.FieldName,
            FieldLabel = sf.FieldLabel,
            DataType = sf.DataType,
            Format = sf.Format,
            IsRequired = sf.IsRequired,
            DisplayOrder = sf.DisplayOrder,
            CreatedAt = sf.CreatedAt
        }).ToList();

        return new SchemaResponse
        {
            Id = schema.Id,
            Name = schema.Name,
            Description = schema.Description,
            Version = schema.Version,
            IsActive = schema.IsActive,
            CreatedAt = schema.CreatedAt,
            UpdatedAt = schema.UpdatedAt,
            CreatedBy = schema.CreatedBy,
            CreatedByName = schema.CreatedByUser.Name,
            SchemaFields = schemaFields
        };
    }
}