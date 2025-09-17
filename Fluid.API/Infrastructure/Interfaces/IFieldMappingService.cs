using Fluid.API.Models.FieldMapping;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IFieldMappingService
{
    /// <summary>
    /// Creates a new field mapping for a project
    /// </summary>
    Task<Result<FieldMappingResponse>> CreateFieldMappingAsync(CreateFieldMappingRequest request, int currentUserId);

    /// <summary>
    /// Creates multiple field mappings for a project in a single transaction
    /// </summary>
    Task<Result<BulkFieldMappingResponse>> CreateBulkFieldMappingsAsync(CreateBulkFieldMappingRequest request, int currentUserId);

    /// <summary>
    /// Gets all field mappings for a specific project
    /// </summary>
    Task<Result<List<FieldMappingResponse>>> GetByProjectIdAsync(int projectId);
}