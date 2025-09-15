using SharedKernel.Result;
using Fluid.API.Models.Batch;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IBatchService
{
    Task<Result<BatchResponse>> CreateAsync(CreateBatchRequest request, int currentUserId);
    Task<Result<BatchResponse>> GetByIdAsync(int id);
    Task<Result<List<BatchListResponse>>> GetAllAsync();
    Task<Result<List<BatchListResponse>>> GetByClientIdAsync(int clientId);
    Task<Result<BatchResponse>> UpdateStatusAsync(int id, UpdateBatchStatusRequest request, int currentUserId);
    Task<Result<BatchResponse>> ProcessBatchAsync(int id, int currentUserId);
    Task<Result<BatchResponse>> ReprocessBatchAsync(ReprocessBatchRequest request, int currentUserId);
    Task<Result<List<BatchOrderResponse>>> GetBatchOrdersAsync(int batchId);
    Task<Result<bool>> DeleteAsync(int id);
}