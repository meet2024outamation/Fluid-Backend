using Fluid.API.Models.Schema;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface ISchemaService
{
    /// <summary>
    /// Creates a new schema with its schema fields
    /// </summary>
    Task<Result<SchemaResponse>> CreateAsync(CreateSchemaRequest request, int currentUserId);

    /// <summary>
    /// Gets all schemas, optionally filtered by client ID
    /// </summary>
    Task<Result<List<SchemaListResponse>>> GetAllAsync(int? clientId = null);

    /// <summary>
    /// Gets a schema by ID with all schema field details
    /// </summary>
    Task<Result<SchemaResponse>> GetByIdAsync(int id);

    /// <summary>
    /// Updates a schema and its schema fields
    /// </summary>
    Task<Result<SchemaResponse>> UpdateAsync(int id, CreateSchemaRequest request, int currentUserId);

    /// <summary>
    /// Updates the status (IsActive) of a schema
    /// </summary>
    Task<Result<SchemaResponse>> UpdateStatusAsync(int id, UpdateSchemaStatusRequest request, int currentUserId);

    /// <summary>
    /// Deletes a schema and all its schema fields
    /// </summary>
    Task<Result<bool>> DeleteAsync(int id);
}