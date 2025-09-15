using Fluid.API.Models.FieldMapping;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IFieldMappingService
{
    /// <summary>
    /// Creates a new field mapping for a client
    /// </summary>
    Task<Result<FieldMappingResponse>> CreateFieldMappingAsync(CreateFieldMappingRequest request, int currentUserId);

    /// <summary>
    /// Creates multiple field mappings for a client in a single transaction
    /// </summary>
    Task<Result<BulkFieldMappingResponse>> CreateBulkFieldMappingsAsync(CreateBulkFieldMappingRequest request, int currentUserId);

    /// <summary>
    /// Gets all field mappings for a specific client
    /// </summary>
    Task<Result<List<FieldMappingResponse>>> GetByClientIdAsync(int clientId);
}