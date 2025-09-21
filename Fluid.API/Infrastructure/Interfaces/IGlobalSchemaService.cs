using Fluid.API.Models.IAMSchema;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

/// <summary>
/// Service interface for managing global schemas in IAM database
/// </summary>
public interface IGlobalSchemaService
{
    /// <summary>
    /// Creates a new global schema with fields
    /// </summary>
    /// <param name="request">Schema creation request</param>
    /// <param name="currentUserId">ID of the user creating the schema</param>
    /// <returns>Created schema details</returns>
    Task<Result<GlobalSchemaResponse>> CreateAsync(CreateGlobalSchemaRequest request, int currentUserId);

    /// <summary>
    /// Gets all global schemas
    /// </summary>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <returns>List of global schemas</returns>
    Task<Result<List<GlobalSchemaListResponse>>> GetAllAsync(bool? isActive = null);

    /// <summary>
    /// Gets a global schema by ID with all fields
    /// </summary>
    /// <param name="id">Schema ID</param>
    /// <returns>Schema details with fields</returns>
    Task<Result<GlobalSchemaResponse>> GetByIdAsync(int id);

    /// <summary>
    /// Updates an existing global schema
    /// </summary>
    /// <param name="id">Schema ID</param>
    /// <param name="request">Update request</param>
    /// <param name="currentUserId">ID of the user updating the schema</param>
    /// <returns>Updated schema details</returns>
    Task<Result<GlobalSchemaResponse>> UpdateAsync(int id, CreateGlobalSchemaRequest request, int currentUserId);

    /// <summary>
    /// Soft deletes a global schema
    /// </summary>
    /// <param name="id">Schema ID</param>
    /// <returns>Success status</returns>
    Task<Result<bool>> DeleteAsync(int id);

    /// <summary>
    /// Activates or deactivates a global schema
    /// </summary>
    /// <param name="id">Schema ID</param>
    /// <param name="isActive">Active status</param>
    /// <returns>Success status</returns>
    Task<Result<bool>> UpdateStatusAsync(int id, bool isActive);

    /// <summary>
    /// Copies a global schema to a specific tenant's database
    /// </summary>
    /// <param name="request">Copy request</param>
    /// <param name="currentUserId">ID of the user copying the schema</param>
    /// <returns>Copy operation result</returns>
    Task<Result<CopySchemaToTenantResponse>> CopySchemaToTenantAsync(CopySchemaToTenantRequest request, int currentUserId);

    /// <summary>
    /// Gets schemas by name (search functionality)
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <returns>List of matching schemas</returns>
    Task<Result<List<GlobalSchemaListResponse>>> SearchAsync(string searchTerm, bool? isActive = null);
}