using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Client;
using Fluid.Entities.Context;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly FluidDbContext _context;
    private readonly ILogger<ClientService> _logger;

    public ClientService(FluidDbContext context, ILogger<ClientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ClientResponse>> CreateAsync(CreateClientRequest request, int currentUserId)
    {
        try
        {
            // Check if client code already exists
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Code == request.Code);

            if (existingClient != null)
            {
                _logger.LogWarning("Attempted to create client with existing code: {Code}", request.Code);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Code),
                    ErrorMessage = $"Client with code '{request.Code}' already exists."
                };

                return Result<ClientResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var client = new Fluid.Entities.Entities.Client
            {
                Name = request.Name,
                Code = request.Code,
                IsActive = request.IsActive,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Load the created client with user information
            var createdClient = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstAsync(c => c.Id == client.Id);

            var response = new ClientResponse
            {
                Id = createdClient.Id,
                Name = createdClient.Name,
                Code = createdClient.Code,
                IsActive = createdClient.IsActive,
                CreatedAt = createdClient.CreatedAt,
                UpdatedAt = createdClient.UpdatedAt,
                CreatedBy = createdClient.CreatedBy,
                CreatedByName = createdClient.CreatedByUser.Name
            };

            _logger.LogInformation("Client created successfully with ID: {ClientId}", createdClient.Id);
            return Result<ClientResponse>.Created(response, "Client created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client with code: {Code}", request.Code);
            return Result<ClientResponse>.Error("An error occurred while creating the client.");
        }
    }

    public async Task<Result<List<ClientListResponse>>> GetAllAsync()
    {
        try
        {
            var clients = await _context.Clients
                .Include(c => c.CreatedByUser)
                .OrderBy(c => c.Name)
                .Select(c => new ClientListResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} clients", clients.Count);
            return Result<List<ClientListResponse>>.Success(clients, $"Retrieved {clients.Count} clients successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return Result<List<ClientListResponse>>.Error("An error occurred while retrieving clients.");
        }
    }

    public async Task<Result<ClientResponse>> GetByIdAsync(int id)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found", id);
                return Result<ClientResponse>.NotFound();
            }

            var response = new ClientResponse
            {
                Id = client.Id,
                Name = client.Name,
                Code = client.Code,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                CreatedBy = client.CreatedBy,
                CreatedByName = client.CreatedByUser.Name
            };

            _logger.LogInformation("Retrieved client with ID: {ClientId}", id);
            return Result<ClientResponse>.Success(response, "Client retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client with ID: {ClientId}", id);
            return Result<ClientResponse>.Error("An error occurred while retrieving the client.");
        }
    }

    public async Task<Result<ClientResponse>> UpdateAsync(int id, UpdateClientRequest request)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for update", id);
                return Result<ClientResponse>.NotFound();
            }

            // Check if the new code conflicts with another client
            if (request.Code != client.Code)
            {
                var existingClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Code == request.Code && c.Id != id);

                if (existingClient != null)
                {
                    _logger.LogWarning("Attempted to update client {ClientId} with existing code: {Code}", id, request.Code);

                    var validationError = new ValidationError
                    {
                        Key = nameof(request.Code),
                        ErrorMessage = $"Client with code '{request.Code}' already exists."
                    };

                    return Result<ClientResponse>.Invalid(new List<ValidationError> { validationError });
                }
            }

            // Update the client properties
            client.Name = request.Name;
            client.Code = request.Code;
            client.IsActive = request.IsActive;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ClientResponse
            {
                Id = client.Id,
                Name = client.Name,
                Code = client.Code,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                CreatedBy = client.CreatedBy,
                CreatedByName = client.CreatedByUser.Name
            };

            _logger.LogInformation("Client updated successfully with ID: {ClientId}", id);
            return Result<ClientResponse>.Success(response, "Client updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client with ID: {ClientId}", id);
            return Result<ClientResponse>.Error("An error occurred while updating the client.");
        }
    }

    public async Task<Result<ClientResponse>> UpdateStatusAsync(int id, UpdateClientStatusRequest request)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for status update", id);
                return Result<ClientResponse>.NotFound();
            }

            // Update only the status
            client.IsActive = request.IsActive;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ClientResponse
            {
                Id = client.Id,
                Name = client.Name,
                Code = client.Code,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                CreatedBy = client.CreatedBy,
                CreatedByName = client.CreatedByUser.Name
            };

            _logger.LogInformation("Client status updated successfully for ID: {ClientId}, IsActive: {IsActive}", id, request.IsActive);
            return Result<ClientResponse>.Success(response, "Client status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client status for ID: {ClientId}", id);
            return Result<ClientResponse>.Error("An error occurred while updating the client status.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.Batches)
                .Include(c => c.FieldMappings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Check if client has associated batches
            if (client.Batches.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Client",
                    ErrorMessage = "Cannot delete client as it has associated batches. Please remove all batches first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if client has associated field mappings
            if (client.FieldMappings.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Client",
                    ErrorMessage = "Cannot delete client as it has associated field mappings. Please remove all field mappings first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Remove the client
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Client deleted successfully with ID: {ClientId}", id);
            return Result<bool>.Success(true, "Client deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client with ID: {ClientId}", id);
            return Result<bool>.Error("An error occurred while deleting the client.");
        }
    }

    public async Task<Result<ClientSchemaAssignmentResponse>> AssignSchemasAsync(AssignSchemasRequest request, int currentUserId)
    {
        try
        {
            // Validate client exists
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == request.ClientId);

            if (client == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.ClientId),
                    ErrorMessage = "Client not found."
                };
                return Result<ClientSchemaAssignmentResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate all schemas exist and are active
            var schemas = await _context.Schemas
                .Where(s => request.SchemaIds.Contains(s.Id))
                .ToListAsync();

            var foundSchemaIds = schemas.Select(s => s.Id).ToList();
            var missingSchemaIds = request.SchemaIds.Except(foundSchemaIds).ToList();

            if (missingSchemaIds.Any())
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.SchemaIds),
                    ErrorMessage = $"Schemas with IDs [{string.Join(", ", missingSchemaIds)}] not found."
                };
                return Result<ClientSchemaAssignmentResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var errors = new List<string>();
            var assignedSchemas = new List<AssignedSchemaInfo>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Remove existing schema assignments for this client
                var existingAssignments = await _context.ClientSchemas
                    .Where(cs => cs.ClientId == request.ClientId)
                    .ToListAsync();

                if (existingAssignments.Any())
                {
                    _context.ClientSchemas.RemoveRange(existingAssignments);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} existing schema assignments for client {ClientId}", 
                        existingAssignments.Count, request.ClientId);
                }

                // Create new schema assignments
                foreach (var schema in schemas)
                {
                    try
                    {
                        var clientSchema = new Fluid.Entities.Entities.ClientSchema
                        {
                            ClientId = request.ClientId,
                            SchemaId = schema.Id,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ClientSchemas.Add(clientSchema);

                        assignedSchemas.Add(new AssignedSchemaInfo
                        {
                            SchemaId = schema.Id,
                            SchemaName = schema.Name,
                            Description = schema.Description,
                            IsActive = schema.IsActive,
                            AssignedAt = clientSchema.CreatedAt
                        });

                        _logger.LogDebug("Assigned schema {SchemaId} to client {ClientId}", schema.Id, request.ClientId);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to assign schema '{schema.Name}' (ID: {schema.Id}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to assign schema {SchemaId} to client {ClientId}", schema.Id, request.ClientId);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new ClientSchemaAssignmentResponse
                {
                    ClientId = request.ClientId,
                    ClientName = client.Name,
                    TotalAssignedSchemas = assignedSchemas.Count,
                    AssignedSchemas = assignedSchemas.OrderBy(a => a.SchemaName).ToList(),
                    Errors = errors
                };

                _logger.LogInformation("Schema assignment completed for client {ClientId}. Assigned: {AssignedCount}, Errors: {ErrorCount}", 
                    request.ClientId, assignedSchemas.Count, errors.Count);

                return Result<ClientSchemaAssignmentResponse>.Success(response, 
                    $"Successfully assigned {assignedSchemas.Count} schemas to client" + 
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
            _logger.LogError(ex, "Error assigning schemas to client {ClientId}", request.ClientId);
            return Result<ClientSchemaAssignmentResponse>.Error("An error occurred while assigning schemas to the client.");
        }
    }
}