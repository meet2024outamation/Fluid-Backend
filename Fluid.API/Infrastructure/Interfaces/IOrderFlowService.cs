using Fluid.API.Models.OrderFlow;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IOrderFlowService
{
    Task<Result<OrderFlowResponse>> CreateAsync(CreateOrderFlowRequest request, int currentUserId);
    Task<Result<OrderFlowResponse>> UpdateAsync(int id, UpdateOrderFlowRequest request, int currentUserId);
    Task<Result<OrderFlowResponse>> GetByIdAsync(int id);
}