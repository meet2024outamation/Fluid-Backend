using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.FieldMapping;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class FieldMappingService : IFieldMappingService
{
    private readonly FluidDbContext _context;
    private readonly ILogger<FieldMappingService> _logger;

    public FieldMappingService(FluidDbContext context, ILogger<FieldMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<FieldMappingResponse>> CreateFieldMappingAsync(CreateFieldMappingRequest request, int currentUserId)
    {
        try
        {
            // Validate project exists
            var project = await _context.Projects
                .FirstOrDefaultAsync(c => c.Id == request.ProjectId);

            if (project == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.ProjectId),
                    ErrorMessage = "Project not found."
                };
                return Result<FieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema exists
            var schema = await _context.Schemas
                .FirstOrDefaultAsync(s => s.Id == request.SchemaId);

            if (schema == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.SchemaId),
                    ErrorMessage = "Schema not found."
                };
                return Result<FieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema field exists
            var schemaField = await _context.SchemaFields
                .FirstOrDefaultAsync(sf => sf.Id == request.SchemaFieldId && sf.SchemaId == request.SchemaId);

            if (schemaField == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.SchemaFieldId),
                    ErrorMessage = "Schema field not found in the specified schema."
                };
                return Result<FieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if mapping already exists for this project, schema, and input field
            var existingMapping = await _context.FieldMappings
                .FirstOrDefaultAsync(fm => fm.ProjectId == request.ProjectId &&
                                          fm.SchemaId == request.SchemaId &&
                                          fm.InputField.ToLower() == request.InputField.ToLower());

            if (existingMapping != null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.InputField),
                    ErrorMessage = $"Field mapping already exists for input field '{request.InputField}' in this project and schema."
                };
                return Result<FieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Create the field mapping
            var fieldMapping = new FieldMapping
            {
                ProjectId = request.ProjectId,
                SchemaId = request.SchemaId,
                SchemaFieldId = request.SchemaFieldId, // Now directly assign int value
                InputField = request.InputField.Trim(),
                Transformation = request.Transformation,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            _context.FieldMappings.Add(fieldMapping);
            await _context.SaveChangesAsync();

            var response = new FieldMappingResponse(
                fieldMapping.Id,
                fieldMapping.ProjectId,
                fieldMapping.SchemaId,
                fieldMapping.SchemaFieldId, // Return the int value directly
                fieldMapping.InputField,
                fieldMapping.Transformation,
                fieldMapping.CreatedAt
            );

            _logger.LogInformation("Field mapping created successfully with ID: {FieldMappingId}", fieldMapping.Id);
            return Result<FieldMappingResponse>.Created(response, "Field mapping created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating field mapping for project {ProjectId}", request.ProjectId);
            return Result<FieldMappingResponse>.Error("An error occurred while creating the field mapping.");
        }
    }

    public async Task<Result<BulkFieldMappingResponse>> CreateBulkFieldMappingsAsync(CreateBulkFieldMappingRequest request, int currentUserId)
    {
        try
        {
            var errors = new List<string>();
            var createdMappings = new List<FieldMappingResponse>();

            // Validate project exists
            var project = await _context.Projects
                .FirstOrDefaultAsync(c => c.Id == request.ProjectId);

            if (project == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.ProjectId),
                    ErrorMessage = "Project not found."
                };
                return Result<BulkFieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate schema exists
            var schema = await _context.Schemas
                .Include(s => s.SchemaFields)
                .FirstOrDefaultAsync(s => s.Id == request.SchemaId);

            if (schema == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.SchemaId),
                    ErrorMessage = "Schema not found."
                };
                return Result<BulkFieldMappingResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate all schema fields exist in the schema
            var validationErrors = new List<ValidationError>();
            var schemaFieldIds = schema.SchemaFields.Select(sf => sf.Id).ToHashSet();

            foreach (var mappingItem in request.FieldMappings)
            {
                if (!schemaFieldIds.Contains(mappingItem.SchemaFieldId))
                {
                    validationErrors.Add(new ValidationError
                    {
                        Key = $"FieldMappings[{request.FieldMappings.IndexOf(mappingItem)}].SchemaFieldId",
                        ErrorMessage = $"Schema field with ID {mappingItem.SchemaFieldId} not found in the specified schema."
                    });
                }
            }

            // Check for duplicate input fields in the request
            var duplicateInputFields = request.FieldMappings
                .GroupBy(m => m.InputField.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicateField in duplicateInputFields)
            {
                validationErrors.Add(new ValidationError
                {
                    Key = nameof(request.FieldMappings),
                    ErrorMessage = $"Duplicate input field '{duplicateField}' found in the request."
                });
            }

            if (validationErrors.Any())
            {
                return Result<BulkFieldMappingResponse>.Invalid(validationErrors);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Remove existing mappings for this project and schema
                var existingMappings = await _context.FieldMappings
                    .Where(fm => fm.ProjectId == request.ProjectId && fm.SchemaId == request.SchemaId)
                    .ToListAsync();

                if (existingMappings.Any())
                {
                    _context.FieldMappings.RemoveRange(existingMappings);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} existing field mappings for project {ProjectId} and schema {SchemaId}",
                        existingMappings.Count, request.ProjectId, request.SchemaId);
                }

                // Create new mappings
                foreach (var mappingItem in request.FieldMappings)
                {
                    try
                    {
                        var fieldMapping = new FieldMapping
                        {
                            ProjectId = request.ProjectId,
                            SchemaId = request.SchemaId,
                            SchemaFieldId = mappingItem.SchemaFieldId,
                            InputField = mappingItem.InputField.Trim(),
                            Transformation = mappingItem.Transformation,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId
                        };

                        _context.FieldMappings.Add(fieldMapping);
                        await _context.SaveChangesAsync();

                        var response = new FieldMappingResponse(
                            fieldMapping.Id,
                            fieldMapping.ProjectId,
                            fieldMapping.SchemaId,
                            fieldMapping.SchemaFieldId,
                            fieldMapping.InputField,
                            fieldMapping.Transformation,
                            fieldMapping.CreatedAt
                        );

                        createdMappings.Add(response);
                        _logger.LogDebug("Created field mapping for input field '{InputField}' -> schema field {SchemaFieldId}",
                            mappingItem.InputField, mappingItem.SchemaFieldId);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to create mapping for field '{mappingItem.InputField}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to create mapping for input field '{InputField}'", mappingItem.InputField);
                    }
                }

                await transaction.CommitAsync();

                var bulkResponse = new BulkFieldMappingResponse(
                    request.ProjectId,
                    request.SchemaId,
                    createdMappings.Count,
                    createdMappings,
                    errors
                );

                _logger.LogInformation("Bulk field mappings created successfully for project {ProjectId}, schema {SchemaId}. Created: {CreatedCount}, Errors: {ErrorCount}",
                    request.ProjectId, request.SchemaId, createdMappings.Count, errors.Count);

                return Result<BulkFieldMappingResponse>.Created(bulkResponse,
                    $"Successfully created {createdMappings.Count} field mappings" +
                    (errors.Any() ? $" with {errors.Count} errors" : ""));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk field mappings for project {ProjectId}", request.ProjectId);
            return Result<BulkFieldMappingResponse>.Error("An error occurred while creating the field mappings.");
        }
    }

    public async Task<Result<List<FieldMappingResponse>>> GetByProjectIdAsync(int projectId)
    {
        try
        {
            var fieldMappings = await _context.FieldMappings
                .Where(fm => fm.ProjectId == projectId)
                .OrderBy(fm => fm.InputField)
                .ToListAsync();

            var responses = fieldMappings.Select(fm => new FieldMappingResponse(
                fm.Id,
                fm.ProjectId,
                fm.SchemaId,
                fm.SchemaFieldId, // Return the int value directly
                fm.InputField,
                fm.Transformation,
                fm.CreatedAt
            )).ToList();

            _logger.LogInformation("Retrieved {Count} field mappings for project {ProjectId}", responses.Count, projectId);
            return Result<List<FieldMappingResponse>>.Success(responses, $"Retrieved {responses.Count} field mappings successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving field mappings for project {ProjectId}", projectId);
            return Result<List<FieldMappingResponse>>.Error("An error occurred while retrieving field mappings.");
        }
    }
}